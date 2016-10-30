// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Linq;
using Framefield.Core;
using Framefield.Core.OperatorPartTraits;
using Framefield.Core.Rendering;
using Framefield.Tooll.Components.SelectionView.ShowScene.CameraInteraction;
using Framefield.Tooll.Components.SelectionView.ShowScene.TransformGizmo;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Utilities = Framefield.Core.Utilities;

namespace Framefield.Tooll
{
    public class D3DRenderSetup : IDisposable
    {
        public Device D3DDevice { get; set; }
        private InputLayout _inputLayout;
        private RenderTargetView _sharedTextureRenderView;
        private RenderTargetView _sceneRenderTargetTextureRenderView;
        private Texture2D _depthTexture;
        private ShaderResourceView _texture;
        private Mesh _screenQuadMesh;
        private int _windowWidth = 1;
        private int _windowHeight = 1;

        public int WindowWidth { get { return _windowWidth; } private set { _windowWidth = Math.Max(1, value); } }
        public int WindowHeight { get { return _windowHeight; } private set { _windowHeight = Math.Max(1, value); } }

        public Operator CurrentCameraOp
        {
            get
            {
                if (Operator != null && Operator.InternalParts.Count > 0)
                {
                    if (Operator.InternalParts[0].Func is ICameraProvider)
                    {
                        return Operator;
                    }
                }
                return null;
            }
        }

        private Texture2D _sharedTexture;
        public Texture2D SharedTexture { get { return _sharedTexture; } }
        private Texture2D _sceneRenderTargetTexture;
        public Texture2D SceneRenderTargetTexture { get { return _sceneRenderTargetTexture; } }
        public Operator Operator { get; set; }

        public Vector3 CameraPosition
        {
            get
            {
                if (Operator != null && Operator.InternalParts.Count > 0)
                {
                    var camOp = Operator.InternalParts[0].Func as ICameraProvider;
                    if (camOp != null)
                    {
                        return camOp.GetLastPosition();
                    }
                }
                return _cameraPosition;
            }
            set
            {
                if (Operator != null && Operator.InternalParts.Count > 0)
                {
                    var camOp = Operator.InternalParts[0].Func as ICameraProvider;
                    if (camOp != null)
                    {
                        camOp.SetPosition(App.Current.Model.GlobalTime, value);
                    }
                }

                _cameraPosition = value;
            }
        }

        public Vector3 CameraTarget
        {
            get
            {
                if (Operator != null && Operator.InternalParts.Count > 0)
                {
                    var camOp = Operator.InternalParts[0].Func as ICameraProvider;
                    if (camOp != null)
                    {
                        return camOp.GetLastTarget();
                    }
                }

                return _cameraTarget;
            }
            set
            {
                if (Operator != null && Operator.InternalParts.Count > 0)
                {
                    var camOp = Operator.InternalParts[0].Func as ICameraProvider;
                    if (camOp != null)
                    {
                        camOp.SetTarget(App.Current.Model.GlobalTime, value);
                    }
                }

                _cameraTarget = value;
            }
        }

        public double CameraRoll
        {
            get
            {
                if (Operator != null && Operator.InternalParts.Count > 0)
                {
                    var camOp = Operator.InternalParts[0].Func as ICameraProvider;
                    if (camOp != null)
                    {
                        return camOp.GetLastRoll();
                    }
                }
                return 0;
            }
            set
            {
                if (Operator != null && Operator.InternalParts.Count > 0)
                {
                    var camOp = Operator.InternalParts[0].Func as ICameraProvider;
                    if (camOp != null)
                    {
                        camOp.SetRoll(App.Current.Model.GlobalTime, value);
                    }
                }
            }
        }


        private Vector3 _cameraPosition = new Vector3(0, 0, CameraInteraction.DEFAULT_CAMERA_POSITION_Z);
        private Vector3 _cameraTarget = Vector3.Zero;

        public D3DRenderSetup(int width, int height)
        {
            WindowWidth = width;
            WindowHeight = height;
            ResetCamera();
            InitTargets();
            _renderer = OperatorPartContext.DefaultRenderer;

            var sceneGridDefinition = MetaManager.Instance.GetMetaOperator(Guid.Parse("a5930d73-3db3-4785-b8c0-991b6fbfe3fc"));
            _sceneGridOperator = sceneGridDefinition.CreateOperator(Guid.Empty);

            var imageBackgroundDefinition = MetaManager.Instance.GetMetaOperator(Guid.Parse("1e4f6cd4-86fa-4c8f-833c-0b8c128cf221"));
            _imageBackgroundOperator = imageBackgroundDefinition.CreateOperator(Guid.Empty);

            TransformGizmo = new TransformGizmo();
        }

        public void Resize(int width, int height)
        {
            DisposeTargets();
            WindowWidth = width;
            WindowHeight = height;
            InitTargets();
        }

        public void ResetCamera()
        {
            CameraPosition = new Vector3(0, 0, CameraInteraction.DEFAULT_CAMERA_POSITION_Z);
            CameraTarget = new Vector3(0, 0, 0);
        }

        public void Dispose()
        {
            DisposeTargets();
            Utilities.DisposeObj(ref _sceneGridOperator);
            Utilities.DisposeObj(ref _imageBackgroundOperator);
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


        private Texture2D _renderDepth;
        private DepthStencilView _renderTargetDepthView;

        public int IndexOfGizmoPartBelowMouse { get; set; }

        public void Render(OperatorPartContext context, bool renderWithGammaCorrection, int outputIdx = 0, bool showGizmos = false, bool renderMesh = false)
        {
            context.D3DDevice = D3DDevice;
            context.Effect = _renderer.SceneDefaultEffect;
            context.InputLayout = _renderer.SceneDefaultInputLayout;
            context.RenderTargetView = renderWithGammaCorrection ? _sceneRenderTargetTextureRenderView : _sharedTextureRenderView;
            context.DepthStencilView = _renderTargetDepthView;
            context.DepthStencilState = _renderer.DefaultDepthStencilState;
            context.BlendState = _renderer.DefaultBlendState;
            context.BlendFactor = _renderer.DefaultBlendFactor;
            context.RasterizerState = _renderer.DefaultRasterizerState;
            context.SamplerState = _renderer.DefaultSamplerState;
            context.Viewport = new Viewport(0, 0, WindowWidth, WindowHeight, 0.0f, 1.0f);
            context.Texture0 = _texture;

            Vector3 viewDir, sideDir, upDir;
            CalcDirections(out viewDir, out sideDir, out upDir);
            Matrix worldToCamera = Matrix.LookAtLH(CameraPosition, CameraTarget, upDir);

            // Find a nice balance between small and large objects (probably skyspheres)
            float zoomLength = (CameraPosition - CameraTarget).Length();
            float farClipping = (zoomLength*2) + 5000;
            float nearClipping = zoomLength/100;

            Setup(context, Operator, _renderer, worldToCamera, (float) nearClipping, (float) farClipping);

            LastContext = context;

            if (showGizmos)
            {
                var selectedOps = (from selectable in App.Current.MainWindow.CompositionView.CompositionGraphView.SelectionHandler.SelectedElements
                                   select selectable as OperatorWidget
                                   into opWidget
                                   where opWidget != null && opWidget.Operator.Outputs.Any()
                                   select opWidget.Operator).ToArray();

                TransformGizmo.SetupEvalCallbackForSelectedTransformOperators(selectedOps, context);

                float? previousValue = null;
                if (context.Variables.ContainsKey(OperatorPartContext.DEBUG_VARIABLE_NAME))
                {
                    previousValue = context.Variables[OperatorPartContext.DEBUG_VARIABLE_NAME];
                }
                context.Variables[OperatorPartContext.DEBUG_VARIABLE_NAME] = 1;

                context.Variables[GIZMO_PART_VARIBALE_NAME] = IndexOfGizmoPartBelowMouse;

                EvaluateOperator(context, outputIdx, renderMesh);

                _sceneGridOperator.Outputs[0].Eval(context);

                TransformGizmo.RenderTransformGizmo(context);

                if (previousValue != null)
                {
                    context.Variables[OperatorPartContext.DEBUG_VARIABLE_NAME] = previousValue.Value;
                }
                else
                {
                    context.Variables.Remove(OperatorPartContext.DEBUG_VARIABLE_NAME);
                }
            }
            else
            {
                EvaluateOperator(context, outputIdx, renderMesh);
            }

            if (renderWithGammaCorrection)
            {
                // in this case we need an additional pass gamma correcting the RT Texture to the shared texture
                context.D3DDevice = D3DDevice;
                context.Effect = _renderer.ScreenRenderGammaCorrectionEffect;
                context.InputLayout = _renderer.ScreenQuadInputLayout;
                context.RenderTargetView = _sharedTextureRenderView;
                context.DepthStencilView = null;
                context.DepthStencilState = _renderer.DefaultDepthStencilState;
                context.BlendState = _renderer.DisabledBlendState; // DefaultBlendState;
                context.BlendFactor = _renderer.DefaultBlendFactor;
                context.RasterizerState = _renderer.DefaultRasterizerState;
                context.SamplerState = _renderer.DefaultSamplerState;
                context.Viewport = new Viewport(0, 0, WindowWidth, WindowHeight, 0.0f, 1.0f);
                context.Texture0 = _texture;

                _renderer.RenderToScreen(_sceneRenderTargetTexture, context);
            }
            _gpuSyncer.Sync(D3DDevice.ImmediateContext);
        }

        private void EvaluateOperator(OperatorPartContext context, int outputIdx, bool renderMesh)
        {
            if (renderMesh)
            {
                Mesh mesh = Operator.Outputs[outputIdx].Eval(context).Mesh;
                context.Renderer.SetupEffect(context);
                context.Renderer.Render(mesh, context);
            }
            else
            {
                // scene renders itself
                Operator.Outputs[outputIdx].Eval(context);
            }
        }

        public const string GIZMO_PART_VARIBALE_NAME = "IndexGizmoPartUnderMouse";


        public void RenderImage(OperatorPartContext context, bool withGammaCorrection, int outputIdx = 0)
        {
            context.D3DDevice = D3DDevice;
            context.Effect = withGammaCorrection ? _renderer.ScreenRenderGammaCorrectionEffect : _renderer.ScreenRenderEffect;
            context.InputLayout = _renderer.ScreenQuadInputLayout;
            context.RenderTargetView = _sharedTextureRenderView;
            context.DepthStencilView = null;
            context.DepthStencilState = _renderer.DefaultDepthStencilState;
            context.BlendState = _renderer.DisabledBlendState; // DefaultBlendState;
            context.BlendFactor = _renderer.DefaultBlendFactor;
            context.RasterizerState = _renderer.DefaultRasterizerState;
            context.SamplerState = _renderer.DefaultSamplerState;
            context.Viewport = new Viewport(0, 0, WindowWidth, WindowHeight, 0.0f, 1.0f);
            context.Texture0 = _texture;

            Vector3 viewDir, sideDir, upDir;
            CalcDirections(out viewDir, out sideDir, out upDir);
            Matrix worldToCamera = Matrix.LookAtLH(CameraPosition, CameraTarget, upDir);

            Setup(context, Operator, _renderer, worldToCamera);

            _imageBackgroundOperator.Outputs[0].Eval(context);
            context.Image = null;

            Texture2D image = Operator.Outputs[outputIdx].Eval(new OperatorPartContext(context)).Image;
            if (image != null)
            {
                _renderer.SetupBaseEffectParameters(context);
                _renderer.RenderToScreen(image, context);
            }
            _gpuSyncer.Sync(D3DDevice.ImmediateContext);
        }

        public static void Setup(OperatorPartContext context, Operator op, DefaultRenderer renderer, Matrix worldToCamera, float nearClipping = 0.01f, float farClipping = 10000)
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
                float aspect = context.Viewport.Width/context.Viewport.Height;
                context.CameraProjection = Matrix.PerspectiveFovLH(3.1415f/4.0f, aspect, nearClipping, farClipping);
            }
            else
            {
                context.WorldToCamera = camOp.GetLastWorldToCamera();
                context.CameraProjection = camOp.GetLastCameraToView();
            }
        }

        // cynic: this method should return a 3x3 matrix instead! in newer sharpdx version this data type is available
        public void CalcDirections(out Vector3 viewDir, out Vector3 sideDir, out Vector3 upDir)
        {
            CalcDirections(CameraTarget, CameraPosition, CameraRoll, out viewDir, out sideDir, out upDir);
        }

        // cynic: this method should return a 3x3 matrix instead! in newer sharpdx version this data type is available
        public static void CalcDirections(Vector3 camTarget, Vector3 camPos, double camRoll,
                                          out Vector3 viewDir, out Vector3 sideDir, out Vector3 upDir)
        {
            viewDir = camTarget - camPos;

            Vector3 worldUp = Vector3.UnitY;
            var roll = (float) camRoll;
            Vector4 rolledUp = Vector3.Transform(worldUp, Matrix.RotationAxis(viewDir, roll));
            rolledUp.Normalize();

            sideDir = Vector3.Cross(rolledUp.ToVector3(), viewDir);
            sideDir.Normalize();

            upDir = Vector3.Cross(viewDir, sideDir);
            upDir.Normalize();
        }

        private void InitTargets()
        {
            try
            {
                D3DDevice = Core.D3DDevice.Device;

                var colorDesc = new Texture2DDescription
                                    {
                                        BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                                        Format = Format.B8G8R8A8_UNorm,
                                        Width = WindowWidth,
                                        Height = WindowHeight,
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
                                        Width = WindowWidth,
                                        Height = WindowHeight,
                                        MipLevels = 1,
                                        SampleDescription = new SampleDescription(1, 0),
                                        Usage = ResourceUsage.Default,
                                        OptionFlags = ResourceOptionFlags.None,
                                        CpuAccessFlags = CpuAccessFlags.None,
                                        ArraySize = 1
                                    };

                _sharedTexture = new Texture2D(D3DDevice, colorDesc);
                colorDesc.OptionFlags = ResourceOptionFlags.None;
                colorDesc.Format = Format.R16G16B16A16_UNorm;
                _sceneRenderTargetTexture = new Texture2D(D3DDevice, colorDesc);
                _depthTexture = new Texture2D(D3DDevice, depthdesc);
                _renderDepth = new Texture2D(D3DDevice, depthdesc);

                _sharedTextureRenderView = new RenderTargetView(D3DDevice, SharedTexture);
                _sceneRenderTargetTextureRenderView = new RenderTargetView(D3DDevice, SceneRenderTargetTexture);
                _renderTargetDepthView = new DepthStencilView(D3DDevice, _renderDepth);

                _texture = ShaderResourceView.FromFile(D3DDevice, "./assets-common/image/white.png");
                _screenQuadMesh = Mesh.CreateScreenQuadMesh();

                _gpuSyncer = new BlockingGpuSyncer(D3DDevice);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }

            D3DDevice.ImmediateContext.Flush();
        }

        private readonly DefaultRenderer _renderer;
        private BlockingGpuSyncer _gpuSyncer;
        private Operator _sceneGridOperator;
        private Operator _imageBackgroundOperator;
        public TransformGizmo TransformGizmo { get; set; }
        public OperatorPartContext LastContext { get; set; }
    }
}
