// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;


namespace Framefield.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector3 PositionRhw;
        public Vector4 Color;
        public Vector4 Color2;
        public Vector2 TexCoord;
        public Vector2 TexCoord2;
    }

    public class Triangle
    {
        public int[] Index = new int[3];
    };



    public class Mesh : IDisposable
    {
        public Buffer Vertices;
        public InputElement[] InputElements { get; set; }
        public int NumTriangles;
        public int AttributesSize { get; set; }

        public void Dispose()
        {
            Utilities.DisposeObj(ref Vertices);
        }

        public static Mesh CreateScreenQuadMesh()
        {
            var inputElements = new[]
                                    {
                                        new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                                        new InputElement("TEXCOORD", 0, Format.R32G32_Float, 16, 0)
                                    };

            const int attributesSize = 24;
            const int streamSize = 2*3*attributesSize;
            using (var vertexStream = new DataStream(streamSize, true, true))
            {
                // tri 1 vert 1
                vertexStream.Write(new Vector4(0.5f, 0.5f, 0, 1));
                vertexStream.Write(new Vector2(1, 0));

                // tri 1 vert 2
                vertexStream.Write(new Vector4(0.5f, -0.5f, 0, 1));
                vertexStream.Write(new Vector2(1, 1));

                // tri 1 vert 3
                vertexStream.Write(new Vector4(-0.5f, -0.5f, 0, 1));
                vertexStream.Write(new Vector2(0, 1));

                // tri 2 vert 1
                vertexStream.Write(new Vector4(-0.5f, -0.5f, 0, 1));
                vertexStream.Write(new Vector2(0, 1));

                // tri 2 vert 2
                vertexStream.Write(new Vector4(-0.5f, 0.5f, 0, 1));
                vertexStream.Write(new Vector2(0, 0));

                // tri 2 vert 3
                vertexStream.Write(new Vector4(0.5f, 0.5f, 0, 1));
                vertexStream.Write(new Vector2(1, 0));

                vertexStream.Position = 0;

                var vertices = new Buffer(D3DDevice.Device, vertexStream, new BufferDescription
                                                                              {
                                                                                  BindFlags = BindFlags.VertexBuffer,
                                                                                  CpuAccessFlags = CpuAccessFlags.None,
                                                                                  OptionFlags = ResourceOptionFlags.None,
                                                                                  SizeInBytes = streamSize,
                                                                                  Usage = ResourceUsage.Default
                                                                              });

                return new Mesh { InputElements = inputElements, Vertices = vertices, NumTriangles = 2, AttributesSize = attributesSize };
            }
        }
    }


    internal static class SmeshReader 
    {
        public static IEnumerable<Mesh> Read(Device d3Ddevice, string smeshFilename)
        {
            var vertexAttributes = new List<VertexAttribute>();

            var doc = XDocument.Load(smeshFilename);
            foreach (XElement x in doc.Elements("Mesh").Elements("Attributes").Elements("Attribute"))
            {
                var id = x.Attribute("id").Value;
                var value = x.Attribute("list").Value;
                var type = (Format) Enum.Parse(typeof(Format), x.Attribute("type").Value);
                System.Diagnostics.Debug.WriteLine("id: {0}  type: {1}", id, type);
                foreach (var listElement in doc.Elements("Mesh").Elements(value))
                {
                    vertexAttributes.Add(VertexAttribute.Create(listElement, id, type));
                }
            }

            // read vertex indices -> each index per vertex is an index to the specific vertex attribute list
            int[][] vertexIndices = null;
            foreach (XElement vertex in doc.Elements("Mesh").Elements("Vertices"))
            {
                var allIndices = vertex.Value.Replace('\n', ' ').Split(new[] { ',' });
                vertexIndices = new int[allIndices.Length][];
                for (int i = 0; i < vertexIndices.Length; ++i)
                {
                    var indicesPerVertex = allIndices[i].Trim().Split(new[] { ' ' });
                    vertexIndices[i] = new int[indicesPerVertex.Length];
                    for (int j = 0; j < vertexAttributes.Count; ++j)
                    {
                        vertexIndices[i][j] = int.Parse(indicesPerVertex[j]);
                    }
                }
            }


            var inputElements = new InputElement[vertexAttributes.Count];
            int offset = 0;
            for (int i = 0; i < vertexAttributes.Count; ++i)
            {
                inputElements[i] = vertexAttributes[i].GetInputElement(ref offset);
            }
            var attributesSize = (from va in vertexAttributes select va.Size).Sum();

            // read objects
            var meshes = new List<Mesh>();
            string[] allFace = null;
            foreach (XElement faces in doc.Elements("Mesh").Elements("Faces"))
            {
                allFace = faces.Value.Replace('\n', ' ').Split(new[] { ',' });
            }
            foreach (XElement objectElement in doc.Elements("Mesh").Elements("Objects"))
            {
                var allObjects = objectElement.Value.Replace('\n', ' ').Split(new[] { ',' });
                Logger.Debug("Reading smesh object with {0} objects...", allObjects.Length);
                foreach (string obj in allObjects)
                {
                    var triangles = new List<Triangle>();

                    var faceIndices = obj.Trim().Split(new[] { ' ' });
                    Logger.Debug(" face index count: {0}", faceIndices.Length);
                    foreach (var faceIdx in faceIndices)
                    {
                        var vertexIndicesList = allFace[int.Parse(faceIdx)];
                        var vertIndices = vertexIndicesList.Trim().Split(new[] { ' ' });

                        var triangle = new Triangle();
                        for (int i = 0; i < 3; ++i)
                            triangle.Index[i] = int.Parse(vertIndices[i]);
                        triangles.Add(triangle);
                        if (vertIndices.Length == 4)
                        {
                            // split quad
                            triangle = new Triangle();
                            triangle.Index[0] = int.Parse(vertIndices[2]);
                            triangle.Index[1] = int.Parse(vertIndices[3]);
                            triangle.Index[2] = int.Parse(vertIndices[0]);
                            triangles.Add(triangle);
                        }
                    }
                    var numTriangles = triangles.Count;

                    int streamSize = triangles.Count*3*attributesSize;

                    using (var vertexStream = new DataStream(streamSize, true, true))
                    {
                        foreach (var tri in triangles)
                        {
                            foreach (var triVertexIdx in tri.Index)
                            {
                                var vi = vertexIndices[triVertexIdx];
                                int attributeIdx = 0;
                                foreach (var index in vi)
                                {
                                    vertexAttributes[attributeIdx++].WriteToStream(vertexStream, index);
                                }
                            }
                        }
                        vertexStream.Position = 0;

                        var vertices = new Buffer(d3Ddevice, vertexStream, new BufferDescription
                                                                               {
                                                                                   BindFlags = BindFlags.VertexBuffer,
                                                                                   CpuAccessFlags = CpuAccessFlags.None,
                                                                                   OptionFlags = ResourceOptionFlags.None,
                                                                                   SizeInBytes = streamSize,
                                                                                   Usage = ResourceUsage.Default
                                                                               });

                        var mesh = new Mesh { InputElements = inputElements, Vertices = vertices, NumTriangles = numTriangles, AttributesSize = attributesSize };
                        meshes.Add(mesh);
                    }
                }
            }

            return meshes;
        }


    }

}
