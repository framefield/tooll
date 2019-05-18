// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Animation;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Framefield.Autodesk.FBX;
using Buffer = SharpDX.Direct3D11.Buffer;

using Newtonsoft.Json;

namespace Framefield.Core
{
    public class Resource : IDisposable
    {
        public Texture2D Texture { get { return _texture; } internal set { _texture = value; } }
        public bool Valid { get; internal set; }
        public OperatorPart User { get; internal set; }

        public void Dispose()
        {
            Utilities.DisposeObj(ref _texture);
            User = null; //we are not the owner of the object, therefor we dont need to dispose the op part.
        }

        Texture2D _texture;
    }

    public abstract class FileResource : IDisposable
    {
        public string Filename { get; internal set; }
        public void Dispose()
        {
            ResourceManager.Dispose(this);
        }

        internal abstract bool IsValid { get; }
        internal abstract void CleanUp();
    }

    internal abstract class FileResourceReader
    {
        internal abstract FileResource ReadFromDisc(string filepath);
    }

    public class MeshResource : FileResource
    {
        public List<Mesh> Meshes { get; internal set; }

        internal override bool IsValid { get { return Meshes != null && Meshes.Count > 0; } }

        internal override void CleanUp()
        {
            foreach (var mesh in Meshes)
            {
                mesh.Dispose();
            }
            Meshes.Clear();
        }
    }

    internal class MeshFileResourceReader : FileResourceReader
    {
        internal override FileResource ReadFromDisc(string filename)
        {
            var meshes = SmeshReader.Read(D3DDevice.Device, filename).ToList();
            return new MeshResource() { Filename = filename, Meshes = meshes };
        }

    }

    public class ImageResource : FileResource
    {
        public Texture2D Image { get { return _image; } set { _image = value; } }

        internal override bool IsValid { get { return Image != null; } }

        internal override void CleanUp()
        {
            Utilities.DisposeObj(ref _image);
        }

        private Texture2D _image = null;
    }

    internal class ImageFileResourceReader : FileResourceReader
    {
        internal override FileResource ReadFromDisc(string filename)
        {
            try
            {
                var image = SharpDX.Direct3D11.Resource.FromFile<Texture2D>(D3DDevice.Device, filename);
                return new ImageResource() { Filename = filename, Image = image };
            }
            catch
            {
                return null;
            }
        }
    }

    public class FbxResource : FileResource
    {
        public Mesh GetChild(int childId, double unitScale)
        {
            return LookupOrCreateChildMesh(childId, unitScale);
        }

        internal FbxResource(Framefield.Autodesk.FBX.Scene fbxScene)
        {
            _fbxScene = fbxScene;
        }

        internal override bool IsValid { get { return _fbxScene != null; } }

        internal override void CleanUp()
        {
            Utilities.DisposeObj(ref _fbxScene);
            foreach (var e in _cachedChildMeshes)
            {
                e.Value.Dispose();
            }
            _cachedChildMeshes.Clear();
        }

        Mesh LookupOrCreateChildMesh(int childId, double unitScale)
        {
            Mesh mesh;
            childId = Utilities.Clamp(childId, -1, _fbxScene.NumObjects - 1);

            var cacheKey = new KeyValuePair<int, double>(childId, unitScale);
            if (!_cachedChildMeshes.TryGetValue(cacheKey, out mesh))
            {
                _fbxScene.UnitScale = unitScale;
                MeshData meshData = _fbxScene.GetMeshFromChild(childId);
                if (meshData != null)
                {
                    var inputElements = new InputElement[]
                                            {
                                                new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32A32_Float, 0, 0),
                                                new InputElement("NORMAL", 0, SharpDX.DXGI.Format.R32G32B32_Float, 16, 0),
                                                new InputElement("COLOR", 0, SharpDX.DXGI.Format.R32G32B32A32_Float, 28, 0),
                                                new InputElement("TEXCOORD", 0, SharpDX.DXGI.Format.R32G32_Float, 44, 0),
                                                new InputElement("TANGENT", 0, SharpDX.DXGI.Format.R32G32B32_Float, 52, 0),
                                                new InputElement("BINORMAL", 0, SharpDX.DXGI.Format.R32G32B32_Float, 64, 0)
                                            };

                    const int attributesSize = 76;
                    int numTriangles = meshData.Vertices.Length/3;

                    using (var vertexStream = new DataStream(meshData.DataBytes, true, true))
                    {
                        vertexStream.Write(meshData.Data, 0, meshData.DataBytes);
                        vertexStream.Position = 0;
                        var vertices = new Buffer(D3DDevice.Device, vertexStream, new BufferDescription()
                                                                                      {
                                                                                          BindFlags = BindFlags.VertexBuffer,
                                                                                          CpuAccessFlags = CpuAccessFlags.None,
                                                                                          OptionFlags = ResourceOptionFlags.None,
                                                                                          SizeInBytes = meshData.DataBytes,
                                                                                          Usage = ResourceUsage.Default
                                                                                      });

                        mesh = new Mesh() { InputElements = inputElements, Vertices = vertices, NumTriangles = numTriangles, AttributesSize = attributesSize };
                        _cachedChildMeshes[cacheKey] = mesh;
                        Logger.Debug("Created child mesh (childId: {0}, scale: {1}, vertices: {2}, triangles: {3})", childId, unitScale, meshData.Vertices.Length, numTriangles);
                    }
                }
            }
            return mesh;
        }

        Framefield.Autodesk.FBX.Scene _fbxScene;
        Dictionary<KeyValuePair<int, double>, Mesh> _cachedChildMeshes = new Dictionary<KeyValuePair<int, double>, Mesh>();
    }

    internal class FbxFileResourceReader : FileResourceReader
    {
        internal override FileResource ReadFromDisc(string filename)
        {
            var fbxScene = Importer.Import(filename, IncludeTransformMatrix);
            return new FbxResource(fbxScene) { Filename = filename };
        }
        public bool IncludeTransformMatrix { get; set; }
    }

    public class RawResource : FileResource
    {
        public Byte[] Data { get; internal set; }

        internal override bool IsValid { get { return Data != null; } }

        internal override void CleanUp()
        {
            Data = null;
        }
    }

    internal class RawFileResourceReader : FileResourceReader
    {
        internal override FileResource ReadFromDisc(string filename)
        {
            var data = File.ReadAllBytes(filename);
            if (data == null)
            {
                throw new Exception(String.Format("resource could not be created from {0}", filename));
            }
            return new RawResource() { Filename = filename, Data = data };
        }
    }

    public static class ResourceManager
    {
        // convinience method for legacy reasons, could be removed (all ops needs to be checked then)
        public static bool ValidateRenderTargetResource(ref Resource resource, OperatorPart user, SharpDX.Direct3D11.Device device, int width, int height, Format imageFormat)
        {
            return ValidateRenderTargetResource(ref resource, user, device, imageFormat, width, height);
        }

        public static bool ValidateRenderTargetResource(ref Resource resource, OperatorPart user, SharpDX.Direct3D11.Device device, int width, int height)
        {
            return ValidateRenderTargetResource(ref resource, user, device, Format.R16G16B16A16_Float, width, height);
        }

        public static bool ValidateRenderTargetResource(ref Resource resource, OperatorPart user, SharpDX.Direct3D11.Device device, Format format, int width, int height)
        {
            var textureDesc = GetTextureDescription(BindFlags.RenderTarget | BindFlags.ShaderResource, format, width, height);
            return ValidateResource(ref resource, user, device, textureDesc);
        }

        //public static bool ValidateRenderTargetResource2(ref Resource resource, OperatorPart user, SharpDX.Direct3D11.Device device, int width, int height)
        //{
        //    var textureDesc = GetTextureDescription(BindFlags.RenderTarget | BindFlags.ShaderResource, Format.R32_Float, width, height);
        //    return ValidateResource(ref resource, user, device, textureDesc);
        //}

        public static bool ValidateDepthStencilResource(ref Resource resource, OperatorPart user, SharpDX.Direct3D11.Device device, int width, int height)
        {
            var depthDesc = GetTextureDescription(BindFlags.DepthStencil | BindFlags.ShaderResource, Format.R32_Typeless, width, height);
            return ValidateResource(ref resource, user, device, depthDesc);
        }

        public static bool ValidateResource(ref Resource resource, OperatorPart user, SharpDX.Direct3D11.Device device, Texture2DDescription textureDesc)
        {
            if (resource != null &&
                resource.Texture != null &&
                resource.Texture.Description.Equals(textureDesc) &&
                _usedResources.Contains(resource))
            {
                return false; //resource has not changed and exists
            }

            InternalDispose(resource); // dispose previous resource (= move to available resources)

            List<Resource> matchingFreeResources = null;
            if (!_freeResources.TryGetValue(textureDesc, out matchingFreeResources))
            {
                var texture = new Texture2D(device, textureDesc);
                matchingFreeResources = new List<Resource>() { new Resource() { Texture = texture, Valid = true } };
            }
            var foundResource = matchingFreeResources[0];
            matchingFreeResources.Remove(foundResource);
            if (matchingFreeResources.Count == 0)
                _freeResources.Remove(textureDesc);

            foundResource.User = user;
            _usedResources.Add(foundResource);

            resource = foundResource;

            return true; //new resource used, return true for changed
        }

        public static void Dispose(Resource resource)
        {
        }

        public static void DisposeAll()
        {
            foreach (var r in _freeResources)
            {
                for (int i = 0; i < r.Value.Count; ++i)
                {
                    var v = r.Value[i];
                    Utilities.DisposeObj(ref v);
                }
            }
            _freeResources.Clear();
            for (int i = 0; i < _usedResources.Count; ++i)
            {
                var v = _usedResources[i];
                Utilities.DisposeObj(ref v);
            }
            _usedResources.Clear();
        }

        private static void InternalDispose(Resource resource)
        {
            if (resource == null || !_usedResources.Contains(resource))
                return;

            if (_freeResources.ContainsKey(resource.Texture.Description))
            {
                _freeResources[resource.Texture.Description].Add(resource);
            }
            else
                _freeResources.Add(resource.Texture.Description, new List<Resource>() { resource });

            _usedResources.Remove(resource);

            resource.Valid = false;
            if (resource.User != null)
                resource.User.EmitChangedEvent();
            resource.User = null;
        }


        public static int NumFreeResources
        {
            get
            {
                int numResources = (from r in _freeResources
                                    from r2 in r.Value
                                    select r2).Count();
                return numResources;
            }
        }

        public static int NumUsedResources
        {
            get
            {
                return _usedResources.Count;
            }
        }

        public static void CheckResources()
        {
            var used = new List<Resource>(_usedResources);
            foreach (var resource in used)
            {
                InternalDispose(resource);
            }
        }

        public static void WriteResourceDescriptions()
        {
            CheckResources();
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;

            using (StreamWriter sw = new StreamWriter("logs/ResourceDescriptions.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartObject();
                writer.WritePropertyName("Resources");
                writer.WriteStartArray();
                foreach (var resourceEntry in _freeResources)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("Description");
                    serializer.Serialize(writer, resourceEntry.Key);
                    string json = JsonConvert.SerializeObject(resourceEntry.Key, Formatting.Indented);
                    writer.WritePropertyName("Num");
                    writer.WriteValue(resourceEntry.Value.Count);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }

        }

        public static Texture2DDescription GetTextureDescription(BindFlags bindFlags, Format format, int width, int height)
        {
            var textureDesc = new Texture2DDescription();
            textureDesc.BindFlags = bindFlags;
            textureDesc.Format = format;
            textureDesc.Width = width;
            textureDesc.Height = height;
            textureDesc.MipLevels = 1;
            textureDesc.SampleDescription = new SampleDescription(1, 0);
            textureDesc.Usage = ResourceUsage.Default;
            textureDesc.OptionFlags = ResourceOptionFlags.None;
            textureDesc.CpuAccessFlags = CpuAccessFlags.None;
            textureDesc.ArraySize = 1;
            return textureDesc;
        }


        // cynic: the watcher event is async, so we need some different mechanism in order not to trigger operators 
        //        changed flags async, which would lead to a great mess. Posponed after soa
        //                 var fileinfo = new FileInfo(filename);
        //                 FileSystemWatcher fileWatcher = new FileSystemWatcher(fileinfo.DirectoryName, fileinfo.Name);
        //                 fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
        //                 fileWatcher.Changed += HandleFileResourceChange;
        //                 fileWatcher.EnableRaisingEvents = true;
        //                 _fileWatchers[filename] = fileWatcher;

        private static FileResource LookUpOrCreateResource(FileResourceReader resourceReader, string filename)
        {
            FileResource fileResource = null;
            if (!_filenameToResourceDict.TryGetValue(filename, out fileResource))
            {
                fileResource = resourceReader.ReadFromDisc(filename);
                if (fileResource != null) {
                    CacheFileResource(fileResource);
                    Logger.Debug("Caching resource {0}...", fileResource.Filename);
                }
            }
            else
            {
                IncreaseRefCountFor(fileResource);
            }
            return fileResource;
        }

        private static ImageFileResourceReader _imageFileResourceReader = new ImageFileResourceReader(); 
        public static ImageResource ReadImage(string filename)
        {
            FileInfo fi = new FileInfo(filename);
            var resource = LookUpOrCreateResource(_imageFileResourceReader, fi.FullName) as ImageResource;
            if (resource == null)
                Logger.Error( "read or cached resource: '{0}' could not be treated as image", filename );
            return resource;
        }

        private static MeshFileResourceReader _meshFileResourceReader = new MeshFileResourceReader();
        public static MeshResource ReadSmesh(string filename)
        {
            FileInfo fi = new FileInfo(filename);
            var resource = LookUpOrCreateResource(_meshFileResourceReader, fi.FullName) as MeshResource;
            if (resource == null)
                Logger.Error("read or cached resource could not be treated as mesh");
            return resource;
        }

        private static FbxFileResourceReader _fbxFileResourceReader = new FbxFileResourceReader();
        public static FbxResource ReadFbx(string filename, bool includeTransformMatrix = true)
        {
            FileInfo fi = new FileInfo(filename);
            _fbxFileResourceReader.IncludeTransformMatrix = includeTransformMatrix;
            var resource = LookUpOrCreateResource(_fbxFileResourceReader, fi.FullName) as FbxResource;
            if (resource == null)
                Logger.Error("read or cached resource could not be treated as fbx");
            return resource;
        }

        private static RawFileResourceReader _rawFileResourceReader = new RawFileResourceReader();
        public static RawResource ReadRaw(string filename)
        {
            FileInfo fi = new FileInfo(filename);
            var resource = LookUpOrCreateResource(_rawFileResourceReader, fi.FullName) as RawResource;
            if (resource == null)
                Logger.Error("read or cached resource could not be treated as raw");
            return resource;
        }

        private static void CacheFileResource(FileResource fileResource)
        {
            if (fileResource.IsValid)
            {
                // only store valid resources in cache
                _fileResourceRefCounters[fileResource] = 1;
                _filenameToResourceDict[fileResource.Filename] = fileResource;
            }
        }

        private static void IncreaseRefCountFor(FileResource fileResource)
        {
            // increase ref count
            int newRefCount = _fileResourceRefCounters[fileResource] + 1;
            _fileResourceRefCounters[fileResource] = newRefCount;
            Logger.Debug("Reference count of cached resource {0} is {1}.", fileResource.Filename, newRefCount);
        }

        internal static void Dispose(FileResource resource)
        {
            if (_fileResourceRefCounters.ContainsKey(resource))
            {
                int newRefCount = _fileResourceRefCounters[resource] - 1;
                _fileResourceRefCounters[resource] = newRefCount;
                Logger.Debug("Reference count of cached resource {0} is {1}.", resource.Filename, newRefCount);
                if (newRefCount == 0)
                {
                    resource.CleanUp();
                    Logger.Debug("Removed resource '{0}' from cache.", resource.Filename);
                    _filenameToResourceDict.Remove(resource.Filename);
                    _fileResourceRefCounters.Remove(resource);
                    resource.Filename = string.Empty;
                }
            }
            else
            {
                Logger.Warn("Trying to dispose a non existing resource '{0}'.", resource.Filename);
            }
        }

        private static Dictionary<Texture2DDescription, List<Resource>> _freeResources = new Dictionary<Texture2DDescription, List<Resource>>();
        private static Dictionary<string, FileResource> _filenameToResourceDict = new Dictionary<string, FileResource>();
        private static Dictionary<FileResource, int> _fileResourceRefCounters = new Dictionary<FileResource, int>();
        private static List<Resource> _usedResources = new List<Resource>();
    }
}
