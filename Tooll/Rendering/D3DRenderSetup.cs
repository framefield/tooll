// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Linq;
using Framefield.Core;
using Framefield.Core.OperatorPartTraits;
using Framefield.Core.Rendering;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Utilities = Framefield.Core.Utilities;


namespace Framefield.Tooll.Rendering
{
    /** Implements the rendering of content within Tooll. Also provides multiple properties to access the used Camera. */
    public class D3DRenderSetup : IDisposable
    {
        public D3DRenderSetup(RenderViewConfiguration renderConfiguration)
        {
            RenderConfig = renderConfiguration;
            InitRenderTargets();
            _renderer = OperatorPartContext.DefaultRenderer;

            var sceneGridDefinition = MetaManager.Instance.GetMetaOperator(Guid.Parse("a5930d73-3db3-4785-b8c0-991b6fbfe3fc"));
            _sceneGridOperator = sceneGridDefinition.CreateOperator(Guid.Empty);

            var imageBackgroundDefinition = MetaManager.Instance.GetMetaOperator(Guid.Parse("1e4f6cd4-86fa-4c8f-833c-0b8c128cf221"));
            _imageBackgroundOperator = imageBackgroundDefinition.CreateOperator(Guid.Empty);

            var cubemapSphereDefinition = MetaManager.Instance.GetMetaOperator(Guid.Parse("276da400-ea2d-4769-9e69-07dd652c928e"));
            _cubemapSphereOperator = cubemapSphereDefinition.CreateOperator(Guid.Empty);

            var plotValueDefinition = MetaManager.Instance.GetMetaOperator(Guid.Parse("0fa9a212-427c-4098-a429-7996ad36be5d"));
            _plotValueOperator = plotValueDefinition.CreateOperator(Guid.Empty);
        }

        public void Dispose()
        {
            DisposeTargets();
            Utilities.DisposeObj(ref _sceneGridOperator);
            Utilities.DisposeObj(ref _imageBackgroundOperator);
            Utilities.DisposeObj(ref _cubemapSphereOperator);
            Utilities.DisposeObj(ref _plotValueOperator);
            Utilities.DisposeObj(ref _D3DImageContainer);
        }


        #region public attributes
        public RenderViewConfiguration RenderConfig { get; set; }
        public Device D3D11Device { get; set; }
        public OperatorPartContext LastContext { get; set; }

        public Texture2D SharedTexture { get { return _sharedTexture; } }
        private Texture2D _sharedTexture;

        public Texture2D SceneRenderTargetTexture { get { return _sceneRenderTargetTexture; } }
        private Texture2D _sceneRenderTargetTexture;
        #endregion



        public void Resize(int width, int height)
        {
            if (width == _sharedTexture.Description.Width
                && height == _sharedTexture.Description.Height)
                return;

            DisposeTargets();
            RenderConfig.Width = width;
            RenderConfig.Height = height;
            InitRenderTargets();
        }


        public D3DImageSharpDX RenderToD3dImage()
        {
            SetupRendering();
            RenderContent();
            return _D3DImageContainer;
        }




        #region content rendering

        public void SetupRendering()
        {
            if (_D3DImageContainer == null)
                _D3DImageContainer = new D3DImageSharpDX();


            CreateContextSettingsWithAspectRatio();
        }


        public void Reinitialize()
        {
            Resize(RenderConfig.Width, RenderConfig.Height);

            CreateContextSettingsWithAspectRatio();

            if (RenderConfig.Operator != null && RenderConfig.Operator.Outputs.Count > 0)
            {
                var invalidator = new OperatorPart.InvalidateVariableAccessors("AspectRatio");
                RenderConfig.Operator.Outputs[0].TraverseWithFunction(null, invalidator);
            }
            RenderContent();
        }


        private void CreateContextSettingsWithAspectRatio()
        {
            _D3DImageContainer.SetBackBufferSharpDX(_sharedTexture);

            var contextSettings = new ContextSettings();
            contextSettings.DisplayMode = new SharpDX.Direct3D9.DisplayMode()
            {
                Width = RenderConfig.Width,
                Height = RenderConfig.Height,
                RefreshRate = 60,
                Format = D3DImageSharpDX.TranslateFormat(_sharedTexture)
            };
            contextSettings.AspectRatio = contextSettings.DisplayMode.AspectRatio;
            _defaultContext = OperatorPartContext.createDefault(contextSettings);
        }


        public void RenderContent()
        {
            if (RenderConfig.Operator == null || RenderConfig.Operator.Outputs.Count <= 0)
                return;

            D3DDevice.BeginFrame();

            try
            {
                var context = new OperatorPartContext(
                                    _defaultContext,
                                    (float)(App.Current.Model.GlobalTime + RenderConfig.TimeScrubOffset));

                var invalidator = new OperatorPart.InvalidateInvalidatables();
                RenderConfig.Operator.Outputs[RenderConfig.ShownOutputIndex].TraverseWithFunctionUseSpecificBehavior(null, invalidator);

                var evaluationType = RenderConfig.Operator.Outputs[RenderConfig.ShownOutputIndex].Type;

                switch (evaluationType)
                {
                    case FunctionType.Float:
                        RenderValuePlot(context, RenderConfig);
                        break;

                    case FunctionType.Scene:
                        Action<OperatorPartContext, int> lambdaForScenes = (OperatorPartContext context2, int outputIdx) =>
                        {
                            RenderConfig.Operator.Outputs[outputIdx].Eval(context);
                        };
                        RenderGeometry(
                            context,
                            lambdaForScenes);

                        break;

                    case FunctionType.Mesh:
                        {
                            Action<OperatorPartContext, int> lambdaForMeshes = (OperatorPartContext context2, int outputIdx) =>
                            {
                                var mesh = RenderConfig.Operator.Outputs[outputIdx].Eval(context2).Mesh;
                                context2.Renderer.SetupEffect(context2);
                                context2.Renderer.Render(mesh, context2);
                            };
                            RenderGeometry(
                                context,
                                lambdaForMeshes);
                            break;
                        }

                    case FunctionType.Image:
                        SetupContextForRenderingImage(
                            context,
                            RenderConfig.RenderWithGammaCorrection);

                        var image = RenderConfig.Operator.Outputs[RenderConfig.ShownOutputIndex].Eval(new OperatorPartContext(context)).Image;
                        if (image == null)
                            break;

                        RenderedImageIsACubemap = image.Description.ArraySize > 1;
                        var cubeMapSide = RenderedImageIsACubemap ? RenderConfig.PreferredCubeMapSideIndex : -1;
                        if (cubeMapSide == 6)
                        {
                            RenderCubemapAsSphere(image, context);
                        }
                        else
                        {
                            RenderImage(image, context);
                        }
                        break;
                }
                _D3DImageContainer.InvalidateD3DImage();

            }
            catch (Exception exception)
            {
                Logger.Error(exception.ToString());
            }
            D3DDevice.EndFrame();
        }


        /** TOM: why do a separate this from the normal dispose-method? */
        //public void CleanUp()
        //{
        //    Utilities.DisposeObj(ref _D3DImageContainer);
        //}
        #endregion


        #region rendering geometry
        public void RenderGeometry(OperatorPartContext context, Action<OperatorPartContext, int> evaluateMeshOrScene)
        {
            SetupContextForGeometry(context);

            LastContext = context;  // Make context accessible to read last camera position

            if (RenderConfig.ShowGridAndGizmos)
            {
                // Check the selected operators for attributes that can be show as Gizmo
                var selectedOps = (from selectable in App.Current.MainWindow.CompositionView.CompositionGraphView.SelectionHandler.SelectedElements
                                   select selectable as OperatorWidget
                                       into opWidget
                                   where opWidget != null && opWidget.Operator.Outputs.Any()
                                   select opWidget.Operator).ToArray();

                RenderConfig.TransformGizmo.SetupEvalCallbackForSelectedTransformOperators(selectedOps, context);

                // Override DebugSetting with ShowGizmo-Setting
                var previousDebugSetting = GetDebugSettingFromContextVariables(context);
                context.Variables[OperatorPartContext.DEBUG_VARIABLE_NAME] = 1;
                context.Variables[GIZMO_PART_VARIBALE_NAME] = RenderConfig.TransformGizmo.IndexOfGizmoPartBelowMouse;

                evaluateMeshOrScene(context, RenderConfig.ShownOutputIndex);

                // Render Grid and Gizmos
                _sceneGridOperator.Outputs[0].Eval(context);
                RenderConfig.TransformGizmo.RenderTransformGizmo(context);

                // Reset DebugSetting
                if (previousDebugSetting != null)
                {
                    context.Variables[OperatorPartContext.DEBUG_VARIABLE_NAME] = previousDebugSetting.Value;
                }
                else
                {
                    context.Variables.Remove(OperatorPartContext.DEBUG_VARIABLE_NAME);
                }
            }
            else
            {
                evaluateMeshOrScene(context, RenderConfig.ShownOutputIndex);
            }

            // With gamma correction we need an additional pass gamma correcting the RT Texture to the shared texture
            if (RenderConfig.RenderWithGammaCorrection)
            {
                SetupContextForRenderingImage(context, withGammaCorrection: true);
                _renderer.RenderToScreen(_sceneRenderTargetTexture, context);
            }
            _gpuSyncer.Sync(D3D11Device.ImmediateContext);
        }

        public void RenderValuePlot(OperatorPartContext context, RenderViewConfiguration renderConfig)
        {
            SetupContextForGeometry(context);

            renderConfig.Operator.Outputs[renderConfig.ShownOutputIndex].Eval(context);

            context.Variables[OperatorPartContext.PLOT_FLOAT_VALUE] = context.Value;

            var invalidator = new OperatorPart.InvalidateInvalidatables();
            _plotValueOperator.Outputs[0].TraverseWithFunction(null, invalidator);

            _plotValueOperator.Outputs[0].Eval(context);
            _gpuSyncer.Sync(D3D11Device.ImmediateContext);
        }



        private void SetupContextForGeometry(OperatorPartContext context)
        {
            SetupContextForRenderingGeometry(context, RenderConfig.RenderWithGammaCorrection);

            RenderConfig.CameraSetup.GetViewDirections(out Vector3 viewDir, out Vector3 sideDir, out Vector3 upDir);
            var worldToCamera = Matrix.LookAtLH(RenderConfig.CameraSetup.Position, RenderConfig.CameraSetup.Target, upDir);

            // Find a nice balance between small and large objects (probably skyspheres)
            var zoomLength = (RenderConfig.CameraSetup.Position - RenderConfig.CameraSetup.Target).Length();
            var farClipping = (zoomLength * 2) + 5000;
            var nearClipping = zoomLength / 100;

            SetupContextForRenderingCamToBuffer(context, RenderConfig.Operator, _renderer, worldToCamera, (float)nearClipping,
                (float)farClipping);

            RenderConfig.CameraSetup.LastWorldToCamera = context.WorldToCamera;
            RenderConfig.CameraSetup.LastCameraProjection = context.CameraProjection;
        }


        private static float? GetDebugSettingFromContextVariables(OperatorPartContext context)
        {
            float? previousDebugSetting = null;
            if (context.Variables.ContainsKey(OperatorPartContext.DEBUG_VARIABLE_NAME))
            {
                previousDebugSetting = context.Variables[OperatorPartContext.DEBUG_VARIABLE_NAME];
            }
            return previousDebugSetting;
        }

        #endregion


        public const string GIZMO_PART_VARIBALE_NAME = "IndexGizmoPartUnderMouse";



        #region rendering images
        public void RenderImage(Texture2D image, OperatorPartContext context)
        {
            RenderConfig.CameraSetup.GetViewDirections(out Vector3 viewDir, out Vector3 sideDir, out Vector3 upDir);
            var worldToCamera = Matrix.LookAtLH(RenderConfig.CameraSetup.Position, RenderConfig.CameraSetup.Target, upDir);

            SetupContextForRenderingCamToBuffer(context, RenderConfig.Operator, _renderer, worldToCamera);

            _imageBackgroundOperator.Outputs[0].Eval(context);
            context.Image = null;

            if (RenderConfig.PreferredCubeMapSideIndex > -1)
            {
                context.Variables[OperatorPartContext.PREFERRED_CUBEMAP_SIDE_INDEX] = RenderConfig.PreferredCubeMapSideIndex;
                context.Effect = _renderer.ScreenQuadCubeMapSideEffect;
            }

            _renderer.SetupBaseEffectParameters(context);
            _renderer.RenderToScreen(image, context);

            _gpuSyncer.Sync(D3D11Device.ImmediateContext);
        }


        public void RenderCubemapAsSphere(Texture2D cubeMapImage, OperatorPartContext context)
        {
            // Set cubemap to context
            var texture = new ShaderResourceView(context.D3DDevice, cubeMapImage);
            if (texture == null)
                return;

            context.SkySphereSRV = texture;

            Action<OperatorPartContext, int> lambdaForMeshes = (OperatorPartContext context2, int outputIdx) =>
            {
                _cubemapSphereOperator.Outputs[0].Eval(context);
            };

            RenderGeometry(context, lambdaForMeshes);
        }
        #endregion


        #region helper methods

        private void InitRenderTargets()
        {
            try
            {
                D3D11Device = Core.D3DDevice.Device;

                var colorDesc = new Texture2DDescription
                {
                    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                    Format = Format.B8G8R8A8_UNorm,
                    Width = RenderConfig.Width,
                    Height = RenderConfig.Height,
                    MipLevels = 1,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    OptionFlags = ResourceOptionFlags.Shared,
                    CpuAccessFlags = CpuAccessFlags.None,
                    ArraySize = 1
                };

                var depthdesc = new Texture2DDescription
                {
                    BindFlags = BindFlags.DepthStencil,
                    Format = Format.D32_Float_S8X24_UInt,
                    Width = RenderConfig.Width,
                    Height = RenderConfig.Height,
                    MipLevels = 1,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    OptionFlags = ResourceOptionFlags.None,
                    CpuAccessFlags = CpuAccessFlags.None,
                    ArraySize = 1
                };

                _sharedTexture = new Texture2D(D3D11Device, colorDesc);
                colorDesc.OptionFlags = ResourceOptionFlags.None;
                colorDesc.Format = Format.R16G16B16A16_UNorm;
                _sceneRenderTargetTexture = new Texture2D(D3D11Device, colorDesc);
                _depthTexture = new Texture2D(D3D11Device, depthdesc);
                _renderDepth = new Texture2D(D3D11Device, depthdesc);

                _sharedTextureRenderView = new RenderTargetView(D3D11Device, SharedTexture);
                _sceneRenderTargetTextureRenderView = new RenderTargetView(D3D11Device, SceneRenderTargetTexture);
                _renderTargetDepthView = new DepthStencilView(D3D11Device, _renderDepth);

                _texture = ShaderResourceView.FromFile(D3D11Device, "./assets-common/image/white.png");
                _screenQuadMesh = Mesh.CreateScreenQuadMesh();

                _gpuSyncer = new BlockingGpuSyncer(D3D11Device);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }

            D3D11Device.ImmediateContext.Flush();
        }

        public static void SetupContextForRenderingCamToBuffer(OperatorPartContext context, Operator op, DefaultRenderer renderer, Matrix worldToCamera, float nearClipping = 0.01f, float farClipping = 10000)
        {
            context.D3DDevice.ImmediateContext.OutputMerger.SetTargets(context.DepthStencilView, context.RenderTargetView);
            context.D3DDevice.ImmediateContext.Rasterizer.SetViewport(context.Viewport);

            if (context.DepthStencilView != null)
            {
                context.D3DDevice.ImmediateContext.ClearDepthStencilView(context.DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            }
            context.D3DDevice.ImmediateContext.ClearRenderTargetView(context.RenderTargetView, new Color4(0.4f, 0.4f, 0.4f, 1.0f));
            context.D3DDevice.ImmediateContext.InputAssembler.InputLayout = context.InputLayout;

            if (op == null)
                return;

            ICameraProvider camOp = null;
            if (op.InternalParts.Count > 0)
            {
                camOp = op.InternalParts[0].Func as ICameraProvider;
            }

            if (camOp == null)
            {
                context.WorldToCamera = worldToCamera;
                float aspect = context.Viewport.Width / context.Viewport.Height;
                context.CameraProjection = Matrix.PerspectiveFovLH(3.1415f / 4.0f, aspect, nearClipping, farClipping);
            }
            else
            {
                context.WorldToCamera = camOp.GetLastWorldToCamera();
                context.CameraProjection = camOp.GetLastCameraToView();
            }
        }

        private void SetupContextForRenderingGeometry(OperatorPartContext context, bool withGammaCorrection)
        {
            context.D3DDevice = D3D11Device;
            context.Effect = _renderer.SceneDefaultEffect;
            context.InputLayout = _renderer.SceneDefaultInputLayout;
            context.RenderTargetView = withGammaCorrection
                ? _sceneRenderTargetTextureRenderView
                : _sharedTextureRenderView;
            context.DepthStencilView = _renderTargetDepthView;
            context.DepthStencilState = _renderer.DefaultDepthStencilState;
            context.BlendState = _renderer.DefaultBlendState;
            context.BlendFactor = _renderer.DefaultBlendFactor;
            context.RasterizerState = _renderer.DefaultRasterizerState;
            context.SamplerState = _renderer.DefaultSamplerState;
            context.Viewport = new Viewport(0, 0, RenderConfig.Width, RenderConfig.Height, 0.0f, 1.0f);
            context.Texture0 = _texture;
        }

        public void SetupContextForRenderingImage(OperatorPartContext context, bool withGammaCorrection)
        {
            context.Effect = withGammaCorrection
                ? _renderer.ScreenRenderGammaCorrectionEffect
                : _renderer.ScreenRenderEffect;
            context.D3DDevice = D3D11Device;
            context.InputLayout = _renderer.ScreenQuadInputLayout;
            context.RenderTargetView = _sharedTextureRenderView;
            context.DepthStencilView = null;
            context.DepthStencilState = _renderer.DefaultDepthStencilState;
            context.BlendState = _renderer.DisabledBlendState; // DefaultBlendState;
            context.BlendFactor = _renderer.DefaultBlendFactor;
            context.RasterizerState = _renderer.DefaultRasterizerState;
            context.SamplerState = _renderer.DefaultSamplerState;
            context.Viewport = new Viewport(0, 0, RenderConfig.Width, RenderConfig.Height, 0.0f, 1.0f);
            context.Texture0 = _texture;
        }



        private void DisposeTargets()
        {
            Utilities.DisposeObj(ref _sharedTextureRenderView);
            Utilities.DisposeObj(ref _sceneRenderTargetTextureRenderView);
            Utilities.DisposeObj(ref _renderTargetDepthView);
            Utilities.DisposeObj(ref _inputLayout);
            Utilities.DisposeObj(ref _sharedTexture);
            Utilities.DisposeObj(ref _sceneRenderTargetTexture);
            Utilities.DisposeObj(ref _depthTexture);
            Utilities.DisposeObj(ref _texture);
            Utilities.DisposeObj(ref _renderDepth);
            Utilities.DisposeObj(ref _screenQuadMesh);
            Utilities.DisposeObj(ref _gpuSyncer);
        }
        #endregion


        /** After rendering an image this flag can be used to display UI-elements relevant for CubeMaps */
        public bool RenderedImageIsACubemap { get; private set; }


        public D3DImageSharpDX D3DImageContainer { get { return _D3DImageContainer; } }
        private D3DImageSharpDX _D3DImageContainer;

        public OperatorPartContext DefaultContext { get { return _defaultContext; } }
        private OperatorPartContext _defaultContext;

        private Texture2D _renderDepth;
        private DepthStencilView _renderTargetDepthView;

        private readonly DefaultRenderer _renderer;
        private BlockingGpuSyncer _gpuSyncer;
        private Operator _sceneGridOperator;
        private Operator _imageBackgroundOperator;
        private Operator _cubemapSphereOperator;
        private Operator _plotValueOperator;

        private InputLayout _inputLayout;
        private RenderTargetView _sharedTextureRenderView;
        private RenderTargetView _sceneRenderTargetTextureRenderView;
        private Texture2D _depthTexture;
        private ShaderResourceView _texture;
        private Mesh _screenQuadMesh;
    }
}
