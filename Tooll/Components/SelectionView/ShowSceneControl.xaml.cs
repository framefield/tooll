// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Windows;
using System.Windows.Input;
using Framefield.Core;
using Framefield.Core.OperatorPartTraits;
using Framefield.Core.Profiling;
using Framefield.Tooll.Components.SelectionView.ShowScene.CameraInteraction;

namespace Framefield.Tooll.Components.SelectionView
{
    public partial class ShowSceneControl
    {

        public ShowSceneControl()
        {
            ShowGridAndGizmos = true;

            Loaded += OnLoadedHandler;
            Unloaded += OnUnloadedHandler;
            InitializeComponent();
        }


        private void OnLoadedHandler(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.GotFocus += MainWindow_GotFocusHandler;
            App.Current.MainWindow.ContentRendered += MainWindow_ContentRendered_Handler;
            App.Current.UpdateAfterUserInteractionEvent += App_UpdateAfterUserInteractionHandler;

            KeyDown += KeyDown_Handler;
            KeyUp += KeyUp_Handler;
            LostFocus += LostFocus_Handler;

            App.Current.CompositionTargertRenderingEvent += App_CompositionTargertRenderingHandler;
            App.Current.UpdateRequiredAfterUserInteraction = true;

            if (_D3DImageContainer == null)
                _D3DImageContainer = new D3DImageSharpDX();

            SetupRendering();

            CameraInteraction = new CameraInteraction(this);     // Note: This requires ShowSceneControl to have been loaded
        }


        private void OnUnloadedHandler(object sender, RoutedEventArgs e)
        {
            KeyDown -= KeyDown_Handler;
            KeyUp -= KeyUp_Handler;

            App.Current.MainWindow.GotFocus -= MainWindow_GotFocusHandler;
            App.Current.MainWindow.ContentRendered -= MainWindow_ContentRendered_Handler;

            LostFocus -= LostFocus_Handler;

            if (App.Current != null)
            {
                App.Current.UpdateAfterUserInteractionEvent -= App_UpdateAfterUserInteractionHandler;
                App.Current.CompositionTargertRenderingEvent -= App_CompositionTargertRenderingHandler;

                CameraInteraction.Discard();
            }
            CleanUp();
        }

        #region Event-Handlers
        private void MainWindow_GotFocusHandler(object sender, RoutedEventArgs e)
        {
            ReinitializeWindow();
        }


        private void MouseDown_Handler(object sender, MouseButtonEventArgs e)
        {
            Focus();
            CameraInteraction.HandleMouseDown(e.ChangedButton);

            Mouse.Capture(this);
            e.Handled = true;
        }


        private void MouseUp_Handler(object sender, MouseButtonEventArgs e)
        {
            var wasRightMouseClick= CameraInteraction.HandleMouseUp(e.ChangedButton);


            // Release captured mouse
            if (!CameraInteraction.AnyMouseButtonPressed())
                Mouse.Capture(null);

            if(!wasRightMouseClick)
                e.Handled = true;
        }


        private void MouseWheel_Handler(object sender, MouseWheelEventArgs e)
        {
            CameraInteraction.HandleMouseWheel(e.Delta);
            e.Handled = true;
        }


        private void LostFocus_Handler(object sender, RoutedEventArgs e)
        {
            CameraInteraction.HandleFocusLost();
        }


        public void KeyDown_Handler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!e.IsRepeat)
            {
                CameraInteraction.HandleKeyDown(e);
            }
        }


        public void KeyUp_Handler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_operator == null)
                return;

            if (e.Key == Key.Return || Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                SwitchToFullscreenMode();

            if (CameraInteraction.HandleKeyUp(e))
                return;
        }


        private void MainWindow_ContentRendered_Handler(object sender, EventArgs e)
        {
            ReinitializeWindow();
        }


        private void SizeChanged_Handler(object sender, SizeChangedEventArgs e)
        {
            ReinitializeWindow();
        }


        void App_UpdateAfterUserInteractionHandler(object sender, EventArgs e)
        {
            RenderContent();
        }


        private void App_CompositionTargertRenderingHandler(object source, EventArgs e)
        {
            if (CameraInteraction== null || !CameraInteraction.UpdateAndCheckIfRedrawRequired())
                return;

            // Check wether this is a basic op and works as a Camera
            if (_operator!= null && _operator.InternalParts.Count > 0 && _operator.InternalParts[0].Func is ICameraProvider)
            {
                App.Current.UpdateRequiredAfterUserInteraction = true;
            }
            else
            {
                RenderContent();
            }
        }

        #endregion


        public void SetOperatorAndOutputIndex(Operator op, int outputIndex = 0)
        {
            if (op == null || op.Outputs.Count < outputIndex + 1)
                return;

            _shownOutputIndex = outputIndex;
            _operator = op;
            RenderContent();            
        }


        #region Rendering
        private void SwitchToFullscreenMode()
        {
            var fsView = new FullScreenView(RenderSetup);
        }

        private void ReinitializeWindow()
        {
            if (_renderSetup == null) 
                return;

            _renderSetup.Resize((int)XGrid.ActualWidth, (int)XGrid.ActualHeight);
            _D3DImageContainer.SetBackBufferSharpDX(_renderSetup.SharedTexture);

            var contextSettings = new ContextSettings();
            contextSettings.DisplayMode = new SharpDX.Direct3D9.DisplayMode()
            {
                Width = _renderSetup.WindowWidth,
                Height = _renderSetup.WindowHeight,
                RefreshRate = 60,
                Format = D3DImageSharpDX.TranslateFormat(_renderSetup.SharedTexture)
            };
            contextSettings.AspectRatio = contextSettings.DisplayMode.AspectRatio;

            _defaultContext = OperatorPartContext.createDefault(contextSettings);

            if (_operator != null && _operator.Outputs.Count > 0)
            {
                var invalidator = new OperatorPart.InvalidateVariableAccessors("AspectRatio");
                _operator.Outputs[0].TraverseWithFunction(null, invalidator);
            }

            RenderContent();
        }


        private void SetupRendering()
        {
            XSceneImage.Source = _D3DImageContainer;
            _renderSetup = new D3DRenderSetup((int)XGrid.ActualWidth, (int)XGrid.ActualHeight);
            _D3DImageContainer.SetBackBufferSharpDX(_renderSetup.SharedTexture);

            var contextSettings = new ContextSettings();
            contextSettings.DisplayMode = new SharpDX.Direct3D9.DisplayMode()
            {
                Width = _renderSetup.WindowWidth,
                Height = _renderSetup.WindowHeight,
                RefreshRate = 60,
                Format = D3DImageSharpDX.TranslateFormat(_renderSetup.SharedTexture)
            };
            contextSettings.AspectRatio = contextSettings.DisplayMode.AspectRatio;
            _defaultContext = OperatorPartContext.createDefault(contextSettings);
        }


        private void RenderContent()
        {
            if (!IsVisible)
                return;

            if (_operator == null || _operator.Outputs.Count <= 0)
                return;

            if (TimeLoggingSourceEnabled)
                TimeLogger.BeginFrame(App.Current.Model.GlobalTime);

            D3DDevice.BeginFrame();

            try
            {
                var context = new OperatorPartContext(_defaultContext, (float)App.Current.Model.GlobalTime);

                // FIXME: the following lines are commented out to enable different values for debugOverlay-Variable
                //if (context.Time != _previousTime)
                //{
                var invalidator = new OperatorPart.InvalidateInvalidatables();
                _operator.Outputs[_shownOutputIndex].TraverseWithFunctionUseSpecificBehavior(null, invalidator);
                //_previousTime = context.Time;
                //}

                var evaluationType = _operator.Outputs[_shownOutputIndex].Type;
                switch (evaluationType)
                {
                    case FunctionType.Scene:
                    case FunctionType.Mesh:
                        _renderSetup.Operator = _operator;
                        var isMeshType = evaluationType == FunctionType.Mesh;
                        _renderSetup.Render(context, _shownOutputIndex, ShowGridAndGizmos, isMeshType);
                        _D3DImageContainer.InvalidateD3DImage();
                        break;

                    case FunctionType.Image:
                        _renderSetup.Operator = _operator;
                        _renderSetup.RenderImage(context, _shownOutputIndex);
                        _D3DImageContainer.InvalidateD3DImage();
                        break;
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception.ToString());
            }

            D3DDevice.EndFrame();
            if (TimeLoggingSourceEnabled)
                TimeLogger.EndFrame();
        }
        #endregion


        public void CleanUp()
        {
            Utilities.DisposeObj(ref _D3DImageContainer);
            Utilities.DisposeObj(ref _renderSetup);
        }

        public bool ShowGridAndGizmos { get; set; }
        public bool RenderWithGammaCorrection { get; set; }
        public bool TimeLoggingSourceEnabled { get; set; }
        public D3DRenderSetup RenderSetup { get { return _renderSetup; } set { _renderSetup = value; } }  // make D3DRenderSetup available for Fullscreen-View


        private D3DImageSharpDX _D3DImageContainer;
        private D3DRenderSetup _renderSetup;
        private OperatorPartContext _defaultContext;

        private Operator _operator;
        private int _shownOutputIndex;
        public CameraInteraction CameraInteraction { get; set; }

    }
}