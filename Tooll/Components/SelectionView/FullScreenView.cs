// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Framefield.Core;
using Framefield.Core.Profiling;
using Framefield.Tooll.Components.SelectionView;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using SharpDX.Multimedia;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;

using Point = System.Windows.Point;
using Utilities = Framefield.Core.Utilities;


namespace Framefield.Tooll.Components.SelectionView
{
    public class FullScreenView : RenderForm
    {
        public bool TimeLoggingSourceEnabled { get; set; }

        public double PlaySpeed { get; set; }

        public double CurrentTime
        {
            get
            {
                double elapsed = (double) _globalTime.ElapsedTicks/Stopwatch.Frequency*PlaySpeed;
                _globalTime.Restart();
                _currentTime += elapsed;
                return _currentTime;
            }
            set { _currentTime = value; }
        }

        private double _currentTime;

        public FullScreenView(D3DRenderSetup renderSetup)
        {
            d3DScene = renderSetup;
            App.Current.MainWindow.Hide();
            System.Windows.Forms.Cursor.Hide();

            KeyDown += KeyDownHandler;
            KeyUp += KeyUpHandler;
            MouseDown += MouseDownHandler;
            MouseUp += MouseUpHandler;
            MouseWheel += MouseWheelHandler;
            MouseMove += MouseMoveHandler;
            MouseDoubleClick += MouseDoubleClickHandler;
            CurrentTime = 0;
            PlaySpeed = 0;
            FormBorderStyle = FormBorderStyle.None;


            var settings = new ContextSettings();
            settings.DisplayMode = new SharpDX.Direct3D9.DisplayMode()
            {
                Width = (int)App.Current.ProjectSettings.GetOrSetDefault("Tooll.FullScreen.Resolution.Width",1920),
                Height = (int)App.Current.ProjectSettings.GetOrSetDefault("Tooll.FullScreen.Resolution.Height", 1080),
                RefreshRate = 60,
                Format = SharpDX.Direct3D9.Format.A8R8G8B8
            };
            settings.AspectRatio = settings.DisplayMode.AspectRatio;

            var displayNumber = int.Parse(App.Current.ProjectSettings.GetOrSetDefault("Tooll.FullScreen.DisplayNumber", "1"));
            displayNumber = Utilities.Clamp(displayNumber, 0, Screen.AllScreens.GetLength(0) - 1);
            Location = Screen.AllScreens[displayNumber].WorkingArea.Location;

            CurrentTime = App.Current.Model.GlobalTime;
            PlaySpeed = App.Current.MainWindow.CompositionView.PlaySpeed;
            App.Current.MainWindow.CompositionView.PlaySpeed = 0;

            Run(settings);

            App.Current.Model.GlobalTime = CurrentTime;
            App.Current.MainWindow.CompositionView.PlaySpeed = PlaySpeed;
            
            App.Current.MainWindow.Show();
            App.Current.MainWindow.Activate();
            App.Current.MainWindow.InvalidateVisual();
            App.Current.MainWindow.XCompositionViewDock.Focus();
            System.Windows.Forms.Cursor.Show();
        }



        public void Run(ContextSettings settings)
        {
            SharpDX.RawInput.Device.RegisterDevice(UsagePage.Generic, UsageId.GenericKeyboard, SharpDX.RawInput.DeviceFlags.None);
            SharpDX.RawInput.Device.RegisterDevice(UsagePage.Generic, UsageId.GenericMouse, SharpDX.RawInput.DeviceFlags.None);
            SharpDX.RawInput.Device.RegisterDevice(UsagePage.Generic, UsageId.GenericJoystick, SharpDX.RawInput.DeviceFlags.None);

            Logger.Debug("Start fullscreen with resolution: {0} x {1}...", settings.DisplayMode.Height, settings.DisplayMode.Width);

            Size = new System.Drawing.Size(settings.DisplayMode.Width, settings.DisplayMode.Height);
            _defaultContext = OperatorPartContext.createDefault(settings);

            var desc = new SwapChainDescription()
                       {
                           BufferCount = 3,
                           ModeDescription = new ModeDescription(ClientSize.Width, ClientSize.Height,
                                                                 new Rational(settings.DisplayMode.RefreshRate, 1), Format.R8G8B8A8_UNorm),
                           IsWindowed = false,
                           OutputHandle = Handle,
                           SampleDescription = new SampleDescription(1, 0),
                           SwapEffect = SwapEffect.Discard,
                           Usage = Usage.RenderTargetOutput,
                           Flags = SwapChainFlags.AllowModeSwitch
                       };

            var dxgiDevice = D3DDevice.Device.QueryInterface<SharpDX.DXGI.Device>();
            var factory = dxgiDevice.Adapter.GetParent<Factory>();
            _swapChain = new SwapChain(factory, D3DDevice.Device, desc);

            _swapChain.ResizeBuffers(3, ClientSize.Width, ClientSize.Height,
                                     _swapChain.Description.ModeDescription.Format, _swapChain.Description.Flags);

            using (var texture = Texture2D.FromSwapChain<Texture2D>(_swapChain, 0))
                _renderTargetView = new RenderTargetView(D3DDevice.Device, texture);

            var depthdesc = new Texture2DDescription
                            {
                                BindFlags = BindFlags.DepthStencil,
                                Format = Format.D32_Float_S8X24_UInt,
                                Width = ClientSize.Width,
                                Height = ClientSize.Height,
                                MipLevels = 1,
                                SampleDescription = new SampleDescription(1, 0),
                                Usage = ResourceUsage.Default,
                                OptionFlags = ResourceOptionFlags.None,
                                CpuAccessFlags = CpuAccessFlags.None,
                                ArraySize = 1
                            };

            _depthTex = new Texture2D(D3DDevice.Device, depthdesc);
            _depthStencilView = new DepthStencilView(D3DDevice.Device, _depthTex);

            D3DDevice.Device.ImmediateContext.OutputMerger.SetTargets(_depthStencilView, _renderTargetView);
            _viewport = new ViewportF(0, 0, ClientSize.Width, ClientSize.Height, 0.0f, 1.0f);
            D3DDevice.Device.ImmediateContext.Rasterizer.SetViewport(_viewport);

            _shaderResourceView = ShaderResourceView.FromFile(D3DDevice.Device, "./assets-common/image/white.png");

            _renderer = new DefaultRenderer();

            _globalTime.Start();

            RenderLoop.Run(this, UpdateLocalShownContent);
        }

        private SwapChain _swapChain = null;
        private RenderTargetView _renderTargetView = null;
        private Texture2D _depthTex = null;
        private DepthStencilView _depthStencilView = null;
        private ViewportF _viewport;
        private ShaderResourceView _shaderResourceView = null;
        private DefaultRenderer _renderer = null;

        private readonly Stopwatch _globalTime = new Stopwatch();

        private float _previousTime = 0.0f;

        private void UpdateLocalShownContent()
        {
            //if (_showSceneControl == null)
            //    return;
            //var op = _showSceneControl.Operator;
            //var d3DScene = _showSceneControl.RenderSetup;
            if (d3DScene == null || d3DScene.Operator == null || d3DScene.Operator.Outputs.Count <= 0)
                return;

            var op = d3DScene.Operator;
            D3DDevice.WindowSize = new SharpDX.Size2(Size.Width, Size.Height);
            D3DDevice.TouchWidth = Size.Width;
            D3DDevice.TouchHeight = Size.Height;

            TimeLogger.BeginFrame(CurrentTime);
            D3DDevice.BeginFrame();

            ProcessKeyEvents();

            try
            {
                var context = new OperatorPartContext(_defaultContext, (float) CurrentTime);

                if (Math.Abs(context.Time - _previousTime) > Constants.Epsilon)
                {
                    var invalidator = new OperatorPart.InvalidateInvalidatables();
                    op.Outputs[0].TraverseWithFunctionUseSpecificBehavior(null, invalidator);
                    _previousTime = context.Time;
                }

                context.D3DDevice = D3DDevice.Device;
                context.RenderTargetView = _renderTargetView;
                context.DepthStencilState = _renderer.DefaultDepthStencilState;
                context.BlendState = _renderer.DefaultBlendState;
                context.RasterizerState = _renderer.DefaultRasterizerState;
                context.SamplerState = _renderer.DefaultSamplerState;
                context.Viewport = _viewport;
                context.Texture0 = _shaderResourceView;

                Vector3 viewDir;
                Vector3 sideDir;
                Vector3 upDir;
                D3DRenderSetup.CalcDirections(d3DScene.CameraTarget, d3DScene.CameraPosition, d3DScene.CameraRoll, out viewDir, out sideDir, out upDir);
                var worldToCamera = Matrix.LookAtLH(d3DScene.CameraPosition, d3DScene.CameraTarget, upDir);

                d3DScene.Operator = op;

                switch (op.FunctionType)
                {
                    case FunctionType.Scene:
                        context.Effect = _renderer.SceneDefaultEffect;
                        context.InputLayout = _renderer.SceneDefaultInputLayout;
                        context.DepthStencilView = _depthStencilView;

                        D3DRenderSetup.Setup(context, op, _renderer, worldToCamera);

                        op.Outputs[0].Eval(context);
                        break;
                    case FunctionType.Image:
                        context.Effect = _renderer.ScreenRenderEffect;
                        context.InputLayout = _renderer.ScreenQuadInputLayout;
                        context.DepthStencilView = null;

                        D3DRenderSetup.Setup(context, op, _renderer, worldToCamera);

                        var image = op.Outputs[0].Eval(new OperatorPartContext(context)).Image;
                        if (image != null)
                        {
                            _renderer.SetupBaseEffectParameters(context);
                            _renderer.RenderToScreen(image, context);
                        }
                        break;
                }

                _swapChain.Present(1, PresentFlags.None);
                D3DDevice.EndFrame();
                TimeLogger.EndFrame();
            }
            catch (Exception exception)
            {
                Logger.Error("Exception while in fullscreen:\n", exception.ToString());
            }
        }

        #region interaction stuff

        #region event handler

        private void KeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Stop();
            }
            else
            {
                _pressedKeys.Add(e.KeyCode);
            }
            e.Handled = true;
        }

        private void KeyUpHandler(object sender, KeyEventArgs e)
        {
            _pressedKeys.Remove(e.KeyCode);

            // handle all key stuff that has no modifier key
            if ((e.Modifiers & Keys.Control) == Keys.None)
            {
                double prevPlaySpeed = PlaySpeed;
                switch (e.KeyCode)
                {
                    case Keys.Space:
                        PlaySpeed = Math.Abs(PlaySpeed) > Constants.Epsilon ? 0 : 1;
                        break;
                    case Keys.L:
                        if (PlaySpeed > 0)
                            PlaySpeed *= 2;
                        else
                            PlaySpeed = 1;
                        break;
                    case Keys.J:
                        if (PlaySpeed < 0)
                            PlaySpeed *= 2;
                        else
                            PlaySpeed = -1;
                        break;
                    case Keys.H:
                        PlaySpeed = -1;
                        break;
                    case Keys.K:
                        PlaySpeed = 0;
                        break;
                }
                if (prevPlaySpeed != PlaySpeed)
                {
                    if (Math.Abs(PlaySpeed) < Constants.Epsilon)
                        App.Current.StopStream();
                    else
                        App.Current.PlayStream(PlaySpeed);
                }
            }
            e.Handled = true;
        }

        private Vector3 _moveVelocity = Vector3.Zero;

        private const float FRICTION = 0.8f;
        private const float INITIAL_MOVE_VELOCITY = 0.02f;
        private const float KEY_ACCELERATION = 0.02f;
        private const float MAX_MOVE_VELOCITY = 4.9f;
        private const float MIN_MOVE_VELOCITY = 0.01f;
        private const double ROTATE_MOUSE_SENSIVITY = 250;

        private void ProcessKeyEvents()
        {
            //if (_showSceneControl == null)
            //    return;
            //var d3DScene = _showSceneControl.RenderSetup;

            Vector3 viewDir, sideDir, upDir;
            d3DScene.CalcDirections(out viewDir, out sideDir, out upDir);

            var viewDirLength = viewDir.Length();
            var initialVelocity = _moveVelocity.Length() < Constants.Epsilon ? INITIAL_MOVE_VELOCITY : 0;

            foreach (var key in _pressedKeys)
            {
                switch (key)
                {
                    case Keys.A:
                        _moveVelocity -= sideDir*(KEY_ACCELERATION + initialVelocity);
                        break;
                    case Keys.D:
                        _moveVelocity += sideDir*(KEY_ACCELERATION + initialVelocity);
                        break;
                    case Keys.W:
                        _moveVelocity += viewDir*(KEY_ACCELERATION + initialVelocity)/viewDirLength;
                        break;
                    case Keys.S:
                        _moveVelocity -= viewDir*(KEY_ACCELERATION + initialVelocity)/viewDirLength;
                        break;
                    case Keys.E:
                        _moveVelocity += upDir*(KEY_ACCELERATION + initialVelocity);
                        break;
                    case Keys.X:
                        _moveVelocity -= upDir*(KEY_ACCELERATION + initialVelocity);
                        break;
                    case Keys.F:
                        _moveVelocity = new SharpDX.Vector3(0, 0, 0);
                        d3DScene.ResetCamera();
                        break;
                    case Keys.C:
                        _moveVelocity = new SharpDX.Vector3(0, 0, 0);
                        d3DScene.CameraTarget = new SharpDX.Vector3(0, 0, 0);
                        d3DScene.CameraPosition = -viewDir;
                        break;
                }
            }

            if (_moveVelocity.Length() < MIN_MOVE_VELOCITY)
            {
                _moveVelocity = new Vector3(0, 0, 0);
            }

            if (_moveVelocity.Length() > 0.0 || _leftMouseButtonPressed || _middleMouseButtonPressed)
            {
                if (_moveVelocity.Length() > MAX_MOVE_VELOCITY)
                {
                    var speed = _moveVelocity.Length();
                    _moveVelocity *= MAX_MOVE_VELOCITY/speed;
                }

                d3DScene.CameraPosition += _moveVelocity;
                d3DScene.CameraTarget += _moveVelocity;
                _moveVelocity *= FRICTION;
            }
        }


        private void MouseDownHandler(object sender, MouseEventArgs e)
        {
            Focus();

            switch (e.Button)
            {
                case MouseButtons.Left:
                    _leftMouseButtonPressed = true;
                    break;
                case MouseButtons.Middle:
                    _middleMouseButtonPressed = true;
                    break;
                case MouseButtons.Right:
                    _rightMouseButtonPressed = true;
                    break;
            }

            _lastMousePos = new Point(e.X, e.Y);
        }

        private void MouseUpHandler(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    _leftMouseButtonPressed = false;
                    break;
                case MouseButtons.Middle:
                    _middleMouseButtonPressed = false;
                    break;
                case MouseButtons.Right:
                    _rightMouseButtonPressed = false;
                    break;
            }
        }

        private void MouseMoveHandler(object sender, MouseEventArgs e)
        {
            _lastMousePos = _currentMousePos;
            _currentMousePos = new Point(e.X, e.Y);
            bool altPressed = _pressedKeys.Contains(Keys.Alt);

            if (_leftMouseButtonPressed)
            {
                if (altPressed)
                    RotateAroundTarget();
                else
                    LookAround();
            }
            else if (_middleMouseButtonPressed && altPressed)
            {
                Pan();
            }
            else if (_rightMouseButtonPressed && altPressed)
            {
                Zoom();
            }
        }

        private void MouseDoubleClickHandler(object sender, MouseEventArgs e)
        {
            Stop();
        }

        private void Stop()
        {
            if (_swapChain != null)
                _swapChain.SetFullscreenState(false, null);

            Close();
            CleanUp();
        }

        private void MouseWheelHandler(object sender, MouseEventArgs e)
        {
            //if (_showSceneControl == null)
            //    return;
            //var d3DScene = _showSceneControl.RenderSetup;

            Vector3 viewDir, sideDir, upDir;
            d3DScene.CalcDirections(out viewDir, out sideDir, out upDir);

            viewDir.Normalize();
            viewDir *= 0.001f;

            _moveVelocity += viewDir*e.Delta;
        }

        #endregion

        #region functionality

        private void LookAround()
        {
            //if (_showSceneControl == null)
            //    return;
            //var d3DScene = _showSceneControl.RenderSetup;

            Vector3 viewDir, sideDir, upDir;
            d3DScene.CalcDirections(out viewDir, out sideDir, out upDir);

            var viewDirLength = viewDir.Length();
            viewDir /= viewDirLength;

            var diff = _currentMousePos - _lastMousePos;
            var factorX = (float) (diff.X/Height*ROTATE_MOUSE_SENSIVITY*Math.PI/180.0);
            var factorY = (float) (diff.Y/Height*ROTATE_MOUSE_SENSIVITY*Math.PI/180.0);

            var rotAroundX = Matrix.RotationAxis(sideDir, factorY);
            var rotAroundY = Matrix.RotationAxis(upDir, factorX);
            var rot = Matrix.Multiply(rotAroundX, rotAroundY);
            var newViewDir = Vector3.Transform(viewDir, rot);
            newViewDir.Normalize();

            d3DScene.CameraTarget = d3DScene.CameraPosition + newViewDir.ToVector3()*viewDirLength;
        }


        private void RotateAroundTarget()
        {
            //if (_showSceneControl == null)
            //    return;
            //var d3DScene = _showSceneControl.RenderSetup;

            Vector3 viewDir, sideDir, upDir;
            d3DScene.CalcDirections(out viewDir, out sideDir, out upDir);

            var viewDirLength = viewDir.Length();
            viewDir /= viewDirLength;

            var diff = _currentMousePos - _lastMousePos;
            var factorX = (float) (diff.X/Height*ROTATE_MOUSE_SENSIVITY*Math.PI/180.0);
            var factorY = (float) (diff.Y/Height*ROTATE_MOUSE_SENSIVITY*Math.PI/180.0);

            var rotAroundX = Matrix.RotationAxis(sideDir, factorY);
            var rotAroundY = Matrix.RotationAxis(upDir, factorX);
            var rot = Matrix.Multiply(rotAroundX, rotAroundY);
            var newViewDir = Vector3.Transform(viewDir, rot);
            newViewDir.Normalize();

            d3DScene.CameraPosition = d3DScene.CameraTarget - newViewDir.ToVector3()*viewDirLength;
        }

        private void Pan()
        {
            //if (_showSceneControl == null)
            //    return;
            //var d3DScene = _showSceneControl.RenderSetup;

            Vector3 viewDir, sideDir, upDir;
            d3DScene.CalcDirections(out viewDir, out sideDir, out upDir);

            var diff = _currentMousePos - _lastMousePos;
            var factorX = (float) (-diff.X/Height*10.0);
            var factorY = (float) (diff.Y/Height*10.0);

            sideDir *= factorX;
            upDir *= factorY;

            d3DScene.CameraPosition += sideDir + upDir;
            d3DScene.CameraTarget += sideDir + upDir;
        }

        private void Zoom()
        {
            //if (_showSceneControl == null)
            //    return;
            //var d3DScene = _showSceneControl.RenderSetup;

            Vector3 viewDir, sideDir, upDir;
            d3DScene.CalcDirections(out viewDir, out sideDir, out upDir);

            var diff = _currentMousePos - _lastMousePos;
            var velocity = (float) (-diff.Y/Height*5.0);

            viewDir.Normalize();
            viewDir *= velocity;

            d3DScene.CameraPosition += viewDir;
            d3DScene.CameraTarget += viewDir;
        }

        #endregion

        #region interaction variables

        private readonly HashSet<Keys> _pressedKeys = new HashSet<Keys>();
        private Point _currentMousePos;
        private Point _lastMousePos;
        private bool _leftMouseButtonPressed;
        private bool _middleMouseButtonPressed;
        private bool _rightMouseButtonPressed;

        #endregion

        #endregion

        private OperatorPartContext _defaultContext;
        private D3DRenderSetup d3DScene;

        public void CleanUp()
        {
            //ShowSceneControl = null;

            Utilities.DisposeObj(ref _renderer);
            Utilities.DisposeObj(ref _shaderResourceView);
            Utilities.DisposeObj(ref _depthStencilView);
            Utilities.DisposeObj(ref _depthTex);
            Utilities.DisposeObj(ref _renderTargetView);
            Utilities.DisposeObj(ref _swapChain);
        }
    }
}
