# ==============================================================================
# Maya exporter that exports still smesh format
# Version 0.8 (2007/01/31)
# Copyright (c) 2007 Andreas Rose (andreas@rose.st)  / modified by Thomas Mann 
#
# License
# Permission is hereby granted by the author to copy and distribute this file
# "as is" for any purpose, without fee. Permission is hereby granted by the
# author to use this file for any purpose, without fee.
# The author claims no responsibility for any damage or otherwise undesired
# outcome that this file may cause, and offer no additional assistance on using
# the resource. The author does not guarantee that this file will work as
# expected. The user employs this file at his or her own risk.
#
# Installation
# 1. Copy the script into Maya/bin/plug-ins folder
# 2. Use Window > Settings/Preferences > Plug-in Manager to activate the script
#
# ToDo
# -Currently ascii or binary export can only selected within the script. Binary
#  export is selected by default. This should be selectable in the option menu.
# ==============================================================================

import sys, struct, traceback
import maya.OpenMaya as OpenMaya
import maya.OpenMayaMPx as OpenMayaMPx
import maya.cmds as cmds

import xml.dom.minidom as minidom

kPluginTranslatorTypeName = "sMeshExporter"


class customNodeTranslator(OpenMayaMPx.MPxFileTranslator):
  def __init__(self):
    OpenMayaMPx.MPxFileTranslator.__init__(self)
    
  def haveWriteMethod(self):
    return True
    
  def haveReadMethod(self):
    return False
    
  def filter(self):
    return "*.smesh"
    
  def defaultExtension(self):
    return "smesh"
    
  def writer(self, fileObject, optionString, accessMode):
    try:
      #determine iterator depend on full or selective export
      itDag = None
      if accessMode == OpenMayaMPx.MPxFileTranslator.kExportAccessMode:
        itDag = OpenMaya.MItDag(OpenMaya.MItDag.kDepthFirst, OpenMaya.MFn.kMesh)
      elif accessMode == OpenMayaMPx.MPxFileTranslator.kExportActiveAccessMode:
        activeList = OpenMaya.MSelectionList()
        OpenMaya.MGlobal.getActiveSelectionList(activeList)
        itDag = OpenMaya.MItSelectionList(activeList, OpenMaya.MFn.kMesh)
      else:
        pass
      
      #setup file and writer function
      fullName = fileObject.fullName()
      fileHandle = None
      fileHandle = open(fullName, "w")

      if not writeAscii(fileHandle, itDag):
        print "Exporting failed"
      
      fileHandle.close()

    except:
      sys.stderr.write("Failed to write file information\n")
      raise
      
      
  def reader(self, fileObject, optionString, accessMode):
    pass
    



#writes object into file in ascii stl format
def writeAscii(fileHandle, itDag):

  try:
    dom = minidom.Document()
    meshElement = dom.createElement("Mesh")
    dom.appendChild(meshElement)
  
    ### 
    DEFAULT_COLOR= (1.0, 1.0, 1.0, 1.0)
    DEFAULT_UV= (0,0)
    
    vertexDict=   {}  # {[idPoint, idNormal, idColor, idUV0, idUV1,],  }
    pointsDict=   {}  # {(float, float, float, float): id}
    normalsDict=  {}  # {(float, float, float): id}
    tangentsDict=  {}  # {(float, float, float): id}
    binormalsDict=  {}  # {(float, float, float): id}
    colorsDict=   {DEFAULT_COLOR: 0}  # {(r, g, b, a): id}    
    uvCoordsDict=   {DEFAULT_UV: 0}  # {(r, g, b, a): id}    
    
    faceList = []
    objectFaces = []    # [ [faceID1, faceID2], [],   ]
    colorSetNames =  cmds.polyColorSet( query=True, allColorSets=True )
  
    #export objects
    numExportedObjects = 0
    while not itDag.isDone():
  
        
      dagPath = getDagPath(itDag)
      dagFn   = OpenMaya.MFnDagNode(dagPath)      
      mesh    = OpenMaya.MFnMesh(dagPath)

      print ">> writing: %s ..." % dagPath.fullPathName()

      
      ### collect face defintions ###
      itPolygon = OpenMaya.MItMeshPolygon(dagPath)
    
      points  = OpenMaya.MPointArray()
      normals = OpenMaya.MVectorArray()
      tangents = OpenMaya.MVectorArray()
      binormals = OpenMaya.MVectorArray()
      colors  = OpenMaya.MColorArray()
      
      uList = OpenMaya.MFloatArray()
      vList = OpenMaya.MFloatArray()
    
      uvSetNames = []
      itPolygon.getUVSetNames( uvSetNames  )
      facesInObject = []
      
    
      while not itPolygon.isDone():
        
        itPolygon.getPoints(points, OpenMaya.MSpace.kObject)
        itPolygon.getNormals(normals, OpenMaya.MSpace.kObject)

        if colorSetNames:
          itPolygon.getColors(colors, colorSetNames[0])
          #print "got colors %s" % colors
        else:
          colors= None

        for uvSet in uvSetNames:
          try:
            itPolygon.getUVs(uList, vList, uvSet)
          except Exception, msg:
            print "failed to query uvSet '%s'" % uvSet
            uList = OpenMaya.MFloatArray()
            vList = OpenMaya.MFloatArray()


            

            #print "tangent=%s" % ([tangent.x,tangent.y,tangent.z])
            # Flip z for left-handed coordinate system
            #tangents.append( [tangent.x, tangent.y, -(tangent.z)] )
            #binormals.append( [binormal.x, binormal.y, -(binormal.z)] )
            itFaceVertex.next()
    
    
        ### iterate over face vertices ###
        faceVerticeIndexes = []
        
        i=0
        #for i in range( points.length() ):

        itFaceVertex = OpenMaya.MItMeshFaceVertex( dagPath, itPolygon.polygon() )
        while not itFaceVertex.isDone():

          ### get tangents and binormals
          #uvSets = []
          #itPolygon.getUVSetNames( uvSets )

          ### get position
          p = points[i]
          tPoint= (p.x, p.y, p.z, 1.0)
          if pointsDict.has_key( tPoint ):
            pointIndex = pointsDict[tPoint]
          else:
            pointIndex = len(pointsDict)
            pointsDict[tPoint]= pointIndex
    

          ### get tangent 
          t = itFaceVertex.getTangent( OpenMaya.MSpace.kObject, uvSetNames[0] )
          tangent= (t.x, t.y, t.z)
          if tangentsDict.has_key( tangent ):
            tangentIndex = tangentsDict[tangent]
          else:
            tangentIndex = len(tangentsDict)
            tangentsDict[tangent]= tangentIndex


          ### get binormal
          b = itFaceVertex.getBinormal( OpenMaya.MSpace.kObject, uvSetNames[0] )          
          binormal= (b.x, b.y, b.z)
          if binormalsDict.has_key( binormal ):
            binormalIndex = binormalsDict[binormal]
          else:
            binormalIndex = len(binormalsDict)
            binormalsDict[binormal]= binormalIndex


          ### get normal
          n = normals[i]
          tNormal= (n.x, n.y, n.z)
          if normalsDict.has_key( tNormal ):
            normalIndex = normalsDict[tNormal]
          else:
            normalIndex = len(normalsDict)
            normalsDict[tNormal]= normalIndex
    
          ### get uvs
          if len(uList) > i and len(vList) > i:
            tUV = (float(uList[i]),  float(vList[i]))
            if uvCoordsDict.has_key( tUV ):
              uvCoordIndex = uvCoordsDict[tUV]
            else:
              uvCoordIndex = len(uvCoordsDict)
              uvCoordsDict[tUV]= uvCoordIndex
          else:
            uvCoordIndex = 0
            
          ### export Colors ###
          if not colors:
            tColor= DEFAULT_COLOR
          else:
            c = colors[i]
            #tColor= ( clamp(int(c.r * 255), 0, 255), 
            #          clamp(int(c.g * 255), 0, 255),
            #          clamp(int(c.b * 255), 0, 255), 
            #          clamp(int(c.a * 255), 0, 255))

            # Fix default colors
            if c.r == -1 and c.g == -1 and c.b == -1 and c.a == -1:
                c.r = c.g = c.b = c.a = 1.0
            tColor= ( c.r, 
                      c.g,
                      c.b, 
                      c.a)
    
          if colorsDict.has_key( tColor ):
            colorIndex = colorsDict[tColor]
          else:
            colorIndex = len(colorsDict)
            colorsDict[tColor]= colorIndex
  
          ### write vertex definition
          vertexDef= (pointIndex, normalIndex, colorIndex, uvCoordIndex, tangentIndex, binormalIndex)
          if vertexDict.has_key( vertexDef ):
            vertexIndex = vertexDict[vertexDef]
          else:
            vertexIndex = len(vertexDict)
            vertexDict[vertexDef]= vertexIndex
    
          faceVerticeIndexes.append("%s" % vertexIndex )
          
          ### next vertex
          i+=1
          itFaceVertex.next()
          
        facesInObject.append( len( faceList ) )
        faceList.append(  " ".join(faceVerticeIndexes)  )
        
        itPolygon.next()

      objectFaces.append( facesInObject )

      numExportedObjects += 1
      itDag.next()
  
    ### write vertex def list ###  
    attributesElement= dom.createElement("Attributes")
    meshElement.appendChild(attributesElement)
    
    for id,listName, vtype in [
                        ("POSITION", "Positions", "R32G32B32A32_Float"),
                        ("NORMAL", "Normals", "R32G32B32_Float"),
                        ("COLOR", "Colors", "R32G32B32A32_Float"),
                        ("TEXCOORD", "UVCoords", "R32G32_Float"),
                        ("TANGENT", "Tangents", "R32G32B32_Float"),
                        ("BINORMAL", "Binormals", "R32G32B32_Float"),
                      ]:
      attributeElement= dom.createElement("Attribute")
      attributeElement.setAttribute("id", id )
      attributeElement.setAttribute("type", vtype )
      attributeElement.setAttribute("list", listName )
      attributesElement.appendChild(attributeElement)
    
	### write vertices ###
    verticesElement = dom.createElement("Vertices")
    meshElement.appendChild(verticesElement)

    t= dom.createTextNode( getDictAsList( vertexDict ) )
    verticesElement.appendChild(t)
    
  
    ### write positions ###
    positionsElement = dom.createElement("Positions")
    meshElement.appendChild(positionsElement)

    t= dom.createTextNode( getDictAsList( pointsDict ) )
    positionsElement.appendChild(t)
    

    ### write normals ###
    normalsElement = dom.createElement("Normals")
    meshElement.appendChild(normalsElement)

    t= dom.createTextNode( getDictAsList( normalsDict ) )
    normalsElement.appendChild(t)

    ### write tangents ###
    tangentsElement = dom.createElement("Tangents")
    meshElement.appendChild(tangentsElement)

    t= dom.createTextNode( getDictAsList( tangentsDict ) )
    tangentsElement.appendChild(t)

    ### write binormals ###
    binormalElement = dom.createElement("Binormals")
    meshElement.appendChild(binormalElement)

    t= dom.createTextNode( getDictAsList( binormalsDict ) )
    binormalElement.appendChild(t)


    ### write colors ###
    colorsElement = dom.createElement("Colors")
    meshElement.appendChild(colorsElement)

    t= dom.createTextNode( getDictAsList( colorsDict ) )
    colorsElement.appendChild(t)


    ### write UVCoords ###
    uvCoordsElement = dom.createElement("UVCoords")
    meshElement.appendChild(uvCoordsElement)

    t= dom.createTextNode( getDictAsList( uvCoordsDict ) )
    uvCoordsElement.appendChild(t)




    ### write faces ###
    facesElement = dom.createElement("Faces")
    meshElement.appendChild(facesElement)

    t= dom.createTextNode(", \n".join( faceList ))
    facesElement.appendChild(t)
  

    ### write objects ###
    objectsElement = dom.createElement("Objects")
    meshElement.appendChild(objectsElement)
  
    tlist= []
    for of in objectFaces:
      tlist.append( listToString( of )   )
    
    t= dom.createTextNode( " ,\n".join( tlist) ) 
    objectsElement.appendChild(t)
  

    #fileHandle.write( "<!DOCTYPE StillMesh>\n")
    fileHandle.write( dom.toprettyxml(indent="  ") )  

    
    return True
  except Exception,msg:    
    print "Failed: %s" % msg;
    traceback.print_exc(file=sys.stdout)
    return False


def clamp(v, min, max):
  if v < min:
    return min
  if v > max:
    return max
  return v


def getDictAsList(dict):
  """
  Prints a dictionary like {somestuff:0, somestuff:1} as an list ordered by value
  """
  reversed = {}
  for (k,v) in dict.iteritems():
    reversed[v] = k

  keys= reversed.keys()
  keys.sort()
  t= []

  for k in keys:    
    t.append( listToString( reversed[k] ) )

  return ",\n".join(t)
    
    
def listToString(someList):
    tStringList = []
    for f in list( someList ):    # could be tuple
      if type(f) == float:
        f = round(f,6)
      tStringList.append(str( f ))
    return " ".join( tStringList )
    
  



#small helper function to get DAG path of different iterator types
def getDagPath(iterator):
  dagPath = OpenMaya.MDagPath()
  if isinstance(iterator, OpenMaya.MItDag):
    iterator.getPath(dagPath)    
  elif isinstance(iterator, OpenMaya.MItSelectionList):
    iterator.getDagPath(dagPath)
  return dagPath




#creator
def translatorCreator():
  return OpenMayaMPx.asMPxPtr(customNodeTranslator())

#initialize the script plug-in
def initializePlugin(mobject):
  mplugin = OpenMayaMPx.MFnPlugin(mobject, 'Andreas Rose & Thomas Mann', '0.8', '8.5')
  try:
    mplugin.registerFileTranslator(kPluginTranslatorTypeName, None, translatorCreator)#,
#                     "stlExportOptions", "binary=1;")
  except:
    sys.stderr.write("Failed to register translator: %s" % kPluginTranslatorTypeName)
    raise

#uninitialize the script plug-in
def uninitializePlugin(mobject):
  mplugin = OpenMayaMPx.MFnPlugin(mobject)
  try:
    mplugin.deregisterFileTranslator(kPluginTranslatorTypeName)
  except:
    sys.stderr.write("Failed to deregister translator: %s" % kPluginTranslatorTypeName)
    raise
