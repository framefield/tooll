// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.D3DCompiler;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace Framefield.Core.Rendering
{
    public class BaseRenderer : IRenderer, IDisposable
    {
        public virtual void Dispose()
        {
            Utilities.DisposeObj(ref _sceneDefaultInputLayout);
            Utilities.DisposeObj(ref _screenQuadInputLayout);
            Utilities.DisposeObj(ref _screenRenderEffect);
            Utilities.DisposeObj(ref __screenQuadMesh);
            Utilities.DisposeObj(ref _defaultDepthStencilState);
            Utilities.DisposeObj(ref _defaultRasterizerState);
            Utilities.DisposeObj(ref _defaultSamplerState);
            Utilities.DisposeObj(ref _defaultBlendState);
            Utilities.DisposeObj(ref _disabledBlendState);

            Utilities.DisposeObj(ref _materialConstBuffer);
            Utilities.DisposeObj(ref _pointLightsConstBuffer);
            Utilities.DisposeObj(ref _fogSettingsConstBuffer);
        }

        private Mesh __screenQuadMesh;

        public Mesh _screenQuadMesh
        {
            get
            {
                if (__screenQuadMesh == null)
                    __screenQuadMesh = Mesh.CreateScreenQuadMesh();
                return __screenQuadMesh;
            }
        }


        public static void SetupConstBufferForCS<T>(OperatorPartContext context, T bufferData, ref Buffer buffer, int slot) where T : struct
        {
            using (var data = new DataStream(Marshal.SizeOf(typeof(T)), true, true))
            {
                data.Write(bufferData);
                data.Position = 0;

                if (buffer == null)
                {
                    var bufferDesc = new BufferDescription
                                         {
                                             Usage = ResourceUsage.Default,
                                             SizeInBytes = Marshal.SizeOf(typeof(T)),
                                             BindFlags = BindFlags.ConstantBuffer
                                         };
                    buffer = new Buffer(context.D3DDevice, data, bufferDesc);
                }
                else
                {
                    context.D3DDevice.ImmediateContext.UpdateSubresource(new DataBox(data.DataPointer, 0, 0), buffer, 0);
                }
                context.D3DDevice.ImmediateContext.ComputeShader.SetConstantBuffer(slot, buffer);
            }
        }


        protected void SetupPointLightsConstBuffer(OperatorPartContext context)
        {
            var pointLights = (HashSet<IPointLight>) context.Objects[OperatorPartContext.POINT_LIGHT_CONTAINER_ID];
            var pointSettingsStruct = new PointLightsConstBufferLayout(pointLights);
            using (var data = new DataStream(Marshal.SizeOf(typeof(PointLightsConstBufferLayout)), true, true))
            {
                data.Write(pointSettingsStruct);
                data.Position = 0;

                if (_pointLightsConstBuffer == null)
                {
                    var bufferDesc = new BufferDescription
                                         {
                                             Usage = ResourceUsage.Default,
                                             SizeInBytes = Marshal.SizeOf(typeof(PointLightsConstBufferLayout)),
                                             BindFlags = BindFlags.ConstantBuffer
                                         };
                    _pointLightsConstBuffer = new Buffer(context.D3DDevice, data, bufferDesc);
                }
                else
                {
                    context.D3DDevice.ImmediateContext.UpdateSubresource(new DataBox(data.DataPointer, 0, 0), _pointLightsConstBuffer, 0);
                }
                var constBuffer = context.Effect.GetConstantBufferByName("PointLightsBuffer");
                if (constBuffer != null && constBuffer.IsValid)
                {
                    constBuffer.SetConstantBuffer(_pointLightsConstBuffer);
                }
            }
        }

        protected bool SetupStructuredBuffer<TElement, TElementBufferLayout>(Device device, TElement[] elements,
                                                                             Func<TElement, TElementBufferLayout> bufferLayoutCreator, // generics support only parameterless ctors, so pass in a delegate which creates the struct
                                                                             ref Buffer structuredBuffer, ref ShaderResourceView srv)
                                                                             where TElementBufferLayout : struct
        {
            int elementSizeInBytes = Marshal.SizeOf(typeof(TElementBufferLayout));
            int numElements = elements.Count();
            int bufferSizeInBytes = elementSizeInBytes * numElements;
            if (bufferSizeInBytes == 0)
            {
                Utilities.DisposeObj(ref structuredBuffer);
                Utilities.DisposeObj(ref srv);
                return false;
            }

            if (structuredBuffer == null || structuredBuffer.Description.SizeInBytes != bufferSizeInBytes)
            {
                using (var data = new DataStream(bufferSizeInBytes, true, true))
                {
                    foreach (var structureElement in elements)
                    {
                        data.Write(bufferLayoutCreator(structureElement));
                    }
                    data.Position = 0;

                    Utilities.DisposeObj(ref structuredBuffer);
                    Utilities.DisposeObj(ref srv);
                    var bufferDesc = new BufferDescription
                    {
                        Usage = ResourceUsage.Dynamic,
                        SizeInBytes = elementSizeInBytes * numElements,
                        StructureByteStride = elementSizeInBytes,
                        OptionFlags = ResourceOptionFlags.BufferStructured,
                        CpuAccessFlags = CpuAccessFlags.Write,
                        BindFlags = BindFlags.ShaderResource
                    };
                    structuredBuffer = new Buffer(device, data, bufferDesc);
                    var bufferResource = new ShaderResourceViewDescription.ExtendedBufferResource()
                    {
                        ElementCount = numElements,
                        FirstElement = 0,
                    };
                    var srvDesc = new ShaderResourceViewDescription()
                    {
                        Format = Format.Unknown,
                        Dimension = ShaderResourceViewDimension.Buffer,
                        BufferEx = bufferResource
                    };
                    srv = new ShaderResourceView(device, structuredBuffer, srvDesc);
                }
            }
            else
            {
                DataStream dataStream;
                device.ImmediateContext.MapSubresource(structuredBuffer, MapMode.WriteDiscard, MapFlags.None, out dataStream);
                foreach (var pointLight in elements)
                {
                    dataStream.Write(bufferLayoutCreator(pointLight));
                }
                device.ImmediateContext.UnmapSubresource(structuredBuffer, 0);
            }
            return true;
        }


        public static bool SetupStructuredBuffer2<TElement, TElementBufferLayout>(Device device, TElement[] elements,
                                                                                 Func<TElement, TElementBufferLayout> bufferLayoutCreator, // generics support only parameterless ctors, so pass in a delegate which creates the struct
                                                                                 ref Buffer structuredBuffer,
                                                                                 bool createSRV, ref ShaderResourceView srv,
                                                                                 bool createUAV, ref UnorderedAccessView uav)
                                                                                 where TElementBufferLayout : struct
        {
            int elementSizeInBytes = Marshal.SizeOf(typeof(TElementBufferLayout));
            int numElements = elements.Count();
            int bufferSizeInBytes = elementSizeInBytes * numElements;
            if (bufferSizeInBytes == 0)
            {
                Utilities.DisposeObj(ref structuredBuffer);
                return false;
            }

            if (structuredBuffer == null || structuredBuffer.Description.SizeInBytes != bufferSizeInBytes)
            {
                Utilities.DisposeObj(ref structuredBuffer);
                var bufferDesc = new BufferDescription
                {
                    Usage = ResourceUsage.Default,
                    SizeInBytes = elementSizeInBytes * numElements,
                    StructureByteStride = elementSizeInBytes,
                    OptionFlags = ResourceOptionFlags.BufferStructured,
                    CpuAccessFlags = CpuAccessFlags.None,
                    BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess
                };
                structuredBuffer = new Buffer(device, bufferDesc);

                if (createSRV)
                {
                    Utilities.DisposeObj(ref srv);
                    var bufferResource = new ShaderResourceViewDescription.ExtendedBufferResource
                    {
                        ElementCount = numElements,
                        FirstElement = 0,
                    };
                    var srvDesc = new ShaderResourceViewDescription()
                    {
                        Format = Format.Unknown,
                        Dimension = ShaderResourceViewDimension.Buffer,
                        BufferEx = bufferResource
                    };
                    srv = new ShaderResourceView(device, structuredBuffer, srvDesc);
                }

                if (createUAV)
                {
                    Utilities.DisposeObj(ref uav);
                    var uavDesc = new UnorderedAccessViewDescription
                    {
                        Format = Format.Unknown,
                        Dimension = UnorderedAccessViewDimension.Buffer,
                        Buffer = new UnorderedAccessViewDescription.BufferResource
                        {
                            FirstElement = 0,
                            ElementCount = numElements,
                            Flags = UnorderedAccessViewBufferFlags.None
                        }
                    };
                    uav = new UnorderedAccessView(device, structuredBuffer, uavDesc);
                }
            }

            return true;
        }


        protected void SetupPointLightsStructuredBufferForEffect(OperatorPartContext context, string effectVariableName, ref Buffer pointLightsBuffer, ref ShaderResourceView pointLightsSRV)
        {
            var pointLights = (HashSet<IPointLight>)context.Objects[OperatorPartContext.POINT_LIGHT_CONTAINER_ID];
            if (SetupStructuredBuffer(context.D3DDevice, pointLights.ToArray(), pl => new PointLightBufferLayout(pl), ref pointLightsBuffer, ref pointLightsSRV))
            {
                var pointLightVariable = context.Effect.GetVariableByName(effectVariableName).AsShaderResource();
                pointLightVariable.SetResource(pointLightsSRV);
            }
        }

        protected void SetupPbrPointLightsStructuredBufferForEffect(OperatorPartContext context, string effectVariableName, ref Buffer pointLightsBuffer, ref ShaderResourceView pointLightsSRV)
        {
            var pointLights = (List<IPbrPointLight>)context.Objects[OperatorPartContext.PBR_POINT_LIGHT_CONTAINER_ID];
            bool success = SetupStructuredBuffer(context.D3DDevice, pointLights.ToArray(), pl => new PbrPointLightBufferLayout(pl),
                                                 ref pointLightsBuffer, ref pointLightsSRV);
            var pointLightVariable = context.Effect.GetVariableByName(effectVariableName).AsShaderResource();
            if (pointLightVariable != null)
            {
                pointLightVariable.SetResource(success ? pointLightsSRV : null);
            }
            else
            {
                Logger.Warn("Found no PBR point light effect variable '{0}'.", effectVariableName);
            }
        }

        protected void SetupFogSettingsConstBuffer(OperatorPartContext context)
        {
            var fogSettings = (IFogSettings) context.Objects[OperatorPartContext.FOG_SETTINGS_ID];
            var fogSettingsStruct = new FogSettingsConstBufferLayout(fogSettings);
            using (var data = new DataStream(Marshal.SizeOf(typeof(FogSettingsConstBufferLayout)), true, true))
            {
                data.Write(fogSettingsStruct);
                data.Position = 0;

                if (_fogSettingsConstBuffer == null)
                {
                    var bufferDesc = new BufferDescription
                                         {
                                             Usage = ResourceUsage.Default,
                                             SizeInBytes = Marshal.SizeOf(typeof(FogSettingsConstBufferLayout)),
                                             BindFlags = BindFlags.ConstantBuffer
                                         };
                    _fogSettingsConstBuffer = new Buffer(context.D3DDevice, data, bufferDesc);
                }
                else
                {
                    context.D3DDevice.ImmediateContext.UpdateSubresource(new DataBox(data.DataPointer, 0, 0), this._fogSettingsConstBuffer, 0);
                }
                var constBuffer = context.Effect.GetConstantBufferByName("FogSettings");
                if (constBuffer != null && constBuffer.IsValid)
                {
                    constBuffer.SetConstantBuffer(_fogSettingsConstBuffer);
                }
            }
        }

        protected void SetupMaterialConstBuffer(OperatorPartContext context)
        {
            var material = (IMaterial) context.Objects[OperatorPartContext.MATERIAL_ID];
            var materialData = new MaterialConstBufferLayout(material);
            using (var data = new DataStream(Marshal.SizeOf(typeof(MaterialConstBufferLayout)), true, true))
            {
                data.Write(materialData);
                data.Position = 0;

                if (_materialConstBuffer == null)
                {
                    var bufferDesc = new BufferDescription
                                         {
                                             Usage = ResourceUsage.Default,
                                             SizeInBytes = Marshal.SizeOf(typeof(MaterialConstBufferLayout)),
                                             BindFlags = BindFlags.ConstantBuffer
                                         };
                    _materialConstBuffer = new Buffer(context.D3DDevice, data, bufferDesc);
                }
                else
                {
                    context.D3DDevice.ImmediateContext.UpdateSubresource(new DataBox(data.DataPointer, 0, 0), _materialConstBuffer, 0);
                }
                var constBuffer = context.Effect.GetConstantBufferByName("MaterialBuffer");
                if (constBuffer != null && constBuffer.IsValid)
                {
                    constBuffer.SetConstantBuffer(_materialConstBuffer);
                }
            }
        }

        public virtual void SetupEffect(OperatorPartContext context)
        {
            SetupBaseEffectParameters(context);
        }

        public void SetupBaseEffectParameters(OperatorPartContext context)
        {
//            context.D3DDevice.ImmediateContext.ClearState();
            var effect = context.Effect;

            var v = effect.GetVariableByName("objectToWorldMatrix").AsMatrix();
            if (v != null && v.IsValid)
            {
                v.SetMatrix(context.ObjectTWorld);
                // cynic: hack until all used effects have the objectToWorldMatrix parameter
                v = effect.GetVariableByName("worldToCameraMatrix").AsMatrix();
                v.SetMatrix(context.WorldToCamera);
            }
            else
            {
                v = effect.GetVariableByName("worldToCameraMatrix").AsMatrix();
                v.SetMatrix(context.ObjectTWorld*context.WorldToCamera);
            }

            v = effect.GetVariableByName("cameraToObjectMatrix").AsMatrix();
            if (v != null && v.IsValid)
            {
                var cameraToObject = Matrix.Invert(context.ObjectTWorld*context.WorldToCamera);
                v.SetMatrix(cameraToObject);
            }

            v = effect.GetVariableByName("projMatrix").AsMatrix();
            v.SetMatrix(context.CameraProjection);

            v = effect.GetVariableByName("textureMatrix").AsMatrix();
            if (v != null && v.IsValid)
            {
                v.SetMatrix(context.TextureMatrix);
            }

            var v2 = effect.GetVariableByName("txDiffuse").AsShaderResource();
            if (v2 != null && v2.IsValid)
            {
                v2.SetResource(context.Texture0);
            }

            var v3 = effect.GetVariableByName("cubeMapSideIndex");
            if (v3 != null && v3.IsValid)
            {
                float index = 0;
                context.Variables.TryGetValue(OperatorPartContext.PREFERRED_CUBEMAP_SIDE_INDEX, out index);
                v3.AsScalar().Set((int)index);
            }
        }
        

        public virtual void Render(Mesh mesh, OperatorPartContext context)
        {
            if(mesh != null)
                Render(mesh, context, 0);
        }

        public virtual void Render(Mesh mesh, OperatorPartContext context, int techniqueIdx)
        {            
            if (context.DepthStencilView != null && context.RenderTargetViews != null)
                context.D3DDevice.ImmediateContext.OutputMerger.SetTargets(context.DepthStencilView, context.RenderTargetViews);
            else if (context.DepthStencilView == null && context.RenderTargetViews != null)
                context.D3DDevice.ImmediateContext.OutputMerger.SetTargets(context.RenderTargetViews);
            else if (context.DepthStencilView != null && context.RenderTargetView != null)
                context.D3DDevice.ImmediateContext.OutputMerger.SetTargets(context.DepthStencilView, context.RenderTargetView);
            else if (context.RenderTargetView != null)
                context.D3DDevice.ImmediateContext.OutputMerger.SetTargets(context.RenderTargetView);
            else if (context.DepthStencilView != null)
                context.D3DDevice.ImmediateContext.OutputMerger.SetTargets(context.DepthStencilView, (RenderTargetView) null);

            if (context.BlendState != null)
            {
                context.D3DDevice.ImmediateContext.OutputMerger.BlendState = context.BlendState;
                context.D3DDevice.ImmediateContext.OutputMerger.BlendFactor = context.BlendFactor;
            }

            if (context.DepthStencilState != null)
            {
                context.D3DDevice.ImmediateContext.OutputMerger.DepthStencilState = context.DepthStencilState;
            }

            if (context.RasterizerState != null)
            {
                context.D3DDevice.ImmediateContext.Rasterizer.State = context.RasterizerState;
            }

            context.D3DDevice.ImmediateContext.Rasterizer.SetViewport(context.Viewport);
            context.D3DDevice.ImmediateContext.InputAssembler.InputLayout = context.InputLayout;
            context.D3DDevice.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.D3DDevice.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(mesh.Vertices, mesh.AttributesSize, 0));

            var technique = context.Effect.GetTechniqueByIndex(techniqueIdx);
            for (int i = 0; i < technique.Description.PassCount; ++i)
            {
                technique.GetPassByIndex(i).Apply(context.D3DDevice.ImmediateContext);
                context.D3DDevice.ImmediateContext.Draw(mesh.NumTriangles*3, 0);
            }
        }



        public virtual void RenderToScreen(Texture2D image, OperatorPartContext context)
        {

            var subContext = new OperatorPartContext(context)
                                 {
                                     DepthStencilView = null,
                                     CameraProjection = Matrix.OrthoLH(1, 1, -100, 100),
                                     WorldToCamera = Matrix.LookAtLH(new Vector3(0, 0, -5), new Vector3(0, 0, 0), new Vector3(0, 1, 0)),
                                     InputLayout = ScreenQuadInputLayout,
                                     BlendState = DefaultBlendState,
                                 };

            var shaderResourceViewDescription = new ShaderResourceViewDescription();
            if (image.Description.ArraySize > 1)
            {
                // Assume its a Cube-map
                shaderResourceViewDescription.Format = image.Description.Format;
                shaderResourceViewDescription.Dimension = ShaderResourceViewDimension.Texture2DArray;
                shaderResourceViewDescription.Texture2DArray = new ShaderResourceViewDescription.Texture2DArrayResource
                                                               {
                                                                   ArraySize = image.Description.ArraySize,
                                                                   FirstArraySlice = 0,
                                                                   MipLevels = image.Description.MipLevels,
                                                                   MostDetailedMip = 0
                                                               };
            }
            else if (image.Description.Format == Format.R32_Typeless)
            {
                // Depth-Buffer
                shaderResourceViewDescription.Format = Format.R32_Float;
                shaderResourceViewDescription.Dimension = ShaderResourceViewDimension.Texture2D;
                shaderResourceViewDescription.Texture2D.MipLevels = 1;
            }
            else
            {
                // Normal Texture
                shaderResourceViewDescription.Format = image.Description.Format; 
                shaderResourceViewDescription.Dimension = ShaderResourceViewDimension.Texture2D;
                shaderResourceViewDescription.Texture2D = new ShaderResourceViewDescription.Texture2DResource
                                                          {
                                                              MipLevels = image.Description.MipLevels,
                                                              MostDetailedMip = 0
                                                          };
            }

            using (var shaderResourceView = new ShaderResourceView(context.D3DDevice, image, shaderResourceViewDescription))
            {
                subContext.Texture0 = shaderResourceView;
                SetupBaseEffectParameters(subContext);
                Render(_screenQuadMesh, subContext);
            }
        }

        public InputLayout SceneDefaultInputLayout
        {
            get
            {
                if (_sceneDefaultInputLayout == null && D3DDevice.Device != null)
                {
                    var technique = DefaultRenderer.DefaultEffect.GetTechniqueByIndex(0);
                    var pass = technique.GetPassByIndex(0);
                    var inputElements = new[]
                                            {
                                                new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                                                new InputElement("NORMAL", 0, Format.R32G32B32_Float, 16, 0),
                                                new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 28, 0),
                                                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 44, 0),
                                                new InputElement("TANGENT", 0, Format.R32G32B32_Float, 52, 0),
                                                new InputElement("BINORMAL", 0, Format.R32G32B32_Float, 64, 0)
                                            };
                    _sceneDefaultInputLayout = new InputLayout(D3DDevice.Device, pass.Description.Signature, inputElements);
                }
                return _sceneDefaultInputLayout;
            }
        }
        private InputLayout _sceneDefaultInputLayout;

        private Effect _screenRenderEffect;
        private Effect _screenQuadCubeMapSideEffect;
        private Effect _screenRenderGammaCorrectionEffect;
        private InputLayout _screenQuadInputLayout;

        public InputLayout ScreenQuadInputLayout
        {
            get
            {
                if (_screenQuadInputLayout == null && D3DDevice.Device != null)
                {
                    var technique = ScreenRenderEffect.GetTechniqueByIndex(0);
                    var pass = technique.GetPassByIndex(0);
                    _screenQuadInputLayout = new InputLayout(D3DDevice.Device, pass.Description.Signature, _screenQuadMesh.InputElements);
                }
                return _screenQuadInputLayout;
            }
        }

        public Effect ScreenRenderEffect
        {
            get
            {
                if (_screenRenderEffect == null && D3DDevice.Device != null)
                {
                    using (var bytecode = ShaderBytecode.CompileFromFile("assets-common/fx/ScreenQuad.fx", "fx_5_0"))
                        _screenRenderEffect = new Effect(D3DDevice.Device, bytecode);
                }
                return _screenRenderEffect;
            }
        }

        public Effect ScreenQuadCubeMapSideEffect
        {
            get
            {
                if (_screenQuadCubeMapSideEffect == null && D3DDevice.Device != null)
                {
                    using (var bytecode = ShaderBytecode.CompileFromFile("assets-common/fx/ScreenQuadCubeMapSide.fx", "fx_5_0"))
                        _screenQuadCubeMapSideEffect = new Effect(D3DDevice.Device, bytecode);
                }
                return _screenQuadCubeMapSideEffect;
            }
        }

        public Effect ScreenRenderGammaCorrectionEffect
        {
            get
            {
                if (_screenRenderGammaCorrectionEffect == null && D3DDevice.Device != null)
                {
                    using (var bytecode = ShaderBytecode.CompileFromFile("assets-common/fx/GammaCorrection.fx", "fx_5_0"))
                        _screenRenderGammaCorrectionEffect = new Effect(D3DDevice.Device, bytecode);
                }
                return _screenRenderGammaCorrectionEffect;
            }
        }


        private DepthStencilState _defaultDepthStencilState;

        public DepthStencilState DefaultDepthStencilState
        {
            get
            {
                if (_defaultDepthStencilState == null && D3DDevice.Device != null)
                {
                    var depthStencilDescription = new DepthStencilStateDescription
                                                      {
                                                          IsDepthEnabled = true,
                                                          DepthWriteMask = DepthWriteMask.All,
                                                          DepthComparison = Comparison.Less,
                                                          StencilReadMask = 255,
                                                          StencilWriteMask = 255
                                                      };
                    _defaultDepthStencilState = new DepthStencilState(D3DDevice.Device, depthStencilDescription);
                }
                return _defaultDepthStencilState;
            }
        }

        private DepthStencilState _disabledDepthStencilState;

        public DepthStencilState DisabledDepthStencilState
        {
            get
            {
                if (_disabledDepthStencilState == null && D3DDevice.Device != null)
                {
                    var depthStencilDescription = new DepthStencilStateDescription
                    {
                        IsDepthEnabled = false
                    };
                    _disabledDepthStencilState = new DepthStencilState(D3DDevice.Device, depthStencilDescription);
                }
                return _disabledDepthStencilState;
            }
        }

        private BlendState _defaultBlendState;

        public BlendState DefaultBlendState
        {
            get
            {
                if (_defaultBlendState == null && D3DDevice.Device != null)
                {
                    var blendStateDescription = new BlendStateDescription();
                    blendStateDescription.RenderTarget[0].IsBlendEnabled = true;
                    blendStateDescription.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
                    blendStateDescription.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
                    blendStateDescription.RenderTarget[0].BlendOperation = BlendOperation.Add;
                    blendStateDescription.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
                    blendStateDescription.RenderTarget[0].DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
                    blendStateDescription.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                    blendStateDescription.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                    blendStateDescription.AlphaToCoverageEnable = false;
                    _defaultBlendState = new BlendState(D3DDevice.Device, blendStateDescription);
                }
                return _defaultBlendState;
            }
        }

        private BlendState _disabledBlendState;

        public BlendState DisabledBlendState
        {
            get
            {
                if (_disabledBlendState == null && D3DDevice.Device != null)
                {
                    var blendStateDescription = new BlendStateDescription();
                    blendStateDescription.RenderTarget[0].IsBlendEnabled = false;
                    blendStateDescription.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                    _disabledBlendState = new BlendState(D3DDevice.Device, blendStateDescription);
                }
                return _disabledBlendState;
            }
        }

        public Color4 DefaultBlendFactor
        {
            get { return new Color4(1, 1, 1, 1); }
        }

        private RasterizerState _defaultRasterizerState;

        public RasterizerState DefaultRasterizerState
        {
            get
            {
                if (_defaultRasterizerState == null && D3DDevice.Device != null)
                {
                    var desc = new RasterizerStateDescription
                                   {
                                       FillMode = FillMode.Solid,
                                       CullMode = CullMode.Back,
                                       IsDepthClipEnabled = true
                                   };
                    _defaultRasterizerState = new RasterizerState(D3DDevice.Device, desc);
                }
                return _defaultRasterizerState;
            }
        }

        private SamplerState _defaultSamplerState;

        public SamplerState DefaultSamplerState
        {
            get
            {
                if (_defaultSamplerState == null && D3DDevice.Device != null)
                {
                    var desc = new SamplerStateDescription
                                   {
                                       Filter = Filter.MinMagMipPoint,
                                       AddressU = TextureAddressMode.Clamp,
                                       AddressV = TextureAddressMode.Clamp,
                                       AddressW = TextureAddressMode.Clamp,
                                       MaximumAnisotropy = 16,
                                       ComparisonFunction = Comparison.Never,
                                       MaximumLod = Single.MaxValue
                                   };

                    _defaultSamplerState = new SamplerState(D3DDevice.Device, desc);
                }
                return _defaultSamplerState;
            }
        }

        protected Buffer _materialConstBuffer;
        protected Buffer _pointLightsConstBuffer;
        protected Buffer _fogSettingsConstBuffer;
    }
}
