﻿// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Windows;
using System.Windows.Input;
using Framefield.Core;
using Framefield.Core.OperatorPartTraits;
using Framefield.Core.Profiling;
using Framefield.Tooll.Components.SelectionView.ShowScene.CameraInteraction;
using Framefield.Tooll.Rendering;
using Framefield.Tooll.Components.SelectionView.ShowScene.TransformGizmo;

namespace Framefield.Tooll.Components.SelectionView
{
    /** A UserControl that handles rendering and interacting with different Scene, Mesh and Image-Content.
     * Adds functionality for CameraInteraction. */
    public partial class ShowContentControl
    {
        public ShowContentControl()
        {
            Loaded += OnLoadedHandler;
            Unloaded += OnUnloadedHandler;
            InitializeComponent();
        }


        private void OnLoadedHandler(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.GotFocus += MainWindow_GotFocusHandler;

            // Not sure why this callback is required. Probably to prevent some render update after comming back from full-screen?
            App.Current.MainWindow.ContentRendered += MainWindow_ContentRendered_Handler;
            App.Current.UpdateAfterUserInteractionEvent += App_UpdateAfterUserInteractionHandler;

            KeyDown += KeyDown_Handler;
            KeyUp += KeyUp_Handler;
            LostFocus += LostFocus_Handler;

            App.Current.CompositionTargertRenderingEvent += App_CompositionTargertRenderingHandler;
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }


        /* This requires ShowSceneControl to have been loaded */
        private void LateInit()
        {
            RenderConfiguration = new RenderViewConfiguration()
            {
                ShowGridAndGizmos = true,
                TransformGizmo = new TransformGizmo(),
            };


            _camSetupProvider = new ViewCameraSetupProvider(RenderConfiguration);
            _renderSetup = new D3DRenderSetup(RenderConfiguration);
            SetupRenderer();

            CameraInteraction = new CameraInteraction(
                RenderConfiguration,
                this);
        }



        private void OnUnloadedHandler(object sender, RoutedEventArgs e)
        {
            KeyDown -= KeyDown_Handler;
            KeyUp -= KeyUp_Handler;
            LostFocus -= LostFocus_Handler;

            App.Current.MainWindow.GotFocus -= MainWindow_GotFocusHandler;
            App.Current.MainWindow.ContentRendered -= MainWindow_ContentRendered_Handler;


            if (App.Current != null)
            {
                App.Current.UpdateAfterUserInteractionEvent -= App_UpdateAfterUserInteractionHandler;
                App.Current.CompositionTargertRenderingEvent -= App_CompositionTargertRenderingHandler;

                if (CameraInteraction != null) // DART:BUG FIXED
                    CameraInteraction.Discard();
            }
            //_renderSetup.CleanUp();
            Utilities.DisposeObj(ref _renderSetup);
        }





        /** Called by SelectView */
        public void SetOperatorAndOutputIndex(Operator op, int outputIndex = 0, double timeScrubOffset = 0)
        {
            EnsureLateInitialization();

            if (op == null || op.Outputs.Count < outputIndex + 1)
                return;

            _camSetupProvider.SetSelectedOperator(op);

            RenderConfiguration.TimeScrubOffset = timeScrubOffset;
            RenderConfiguration.ShownOutputIndex = outputIndex;
            RenderConfiguration.Operator = op;
            RenderContent();
        }


        private void EnsureLateInitialization()
        {
            if (_camSetupProvider == null || _renderSetup == null)
                LateInit();
        }


        #region Event-Handlers
        private void MainWindow_GotFocusHandler(object sender, RoutedEventArgs e)
        {
            EnsureLateInitialization();
            _renderSetup.Reinitialize();
        }


        private void MouseDown_Handler(object sender, MouseButtonEventArgs e)
        {
            EnsureLateInitialization();

            Focus();
            CameraInteraction.HandleMouseDown(e.ChangedButton);

            Mouse.Capture(this);
            e.Handled = true;
        }


        private void MouseUp_Handler(object sender, MouseButtonEventArgs e)
        {
            var wasRightMouseClick = CameraInteraction.HandleMouseUp(e.ChangedButton);

            // Release captured mouse
            if (!CameraInteraction.AnyMouseButtonPressed())
                Mouse.Capture(null);

            if (!wasRightMouseClick)
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


        private void KeyDown_Handler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!e.IsRepeat)
            {
                CameraInteraction.HandleKeyDown(e);
            }
        }


        private void KeyUp_Handler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (RenderConfiguration.Operator == null)
                return;

            if (e.Key == Key.Return || Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                SwitchToFullscreenMode();

            if (CameraInteraction.HandleKeyUp(e))
                return;
        }


        private void MainWindow_ContentRendered_Handler(object sender, EventArgs e)
        {
            EnsureLateInitialization();
            _renderSetup.Reinitialize();
        }


        private void SizeChanged_Handler(object sender, SizeChangedEventArgs e)
        {
            if (_renderSetup != null)
            {
                SetRendererSizeFromWindow();
                _renderSetup.Reinitialize();
            }
        }



        private void App_CompositionTargertRenderingHandler(object source, EventArgs e)
        {
            if (CameraInteraction == null)
                return;

            var updateRequiredAfterCameraInteraction = CameraInteraction.UpdateAndCheckIfRedrawRequired();
            if (!updateRequiredAfterCameraInteraction)
            {
                /**
                 * If the current camera is an operator, and its parameter have been modified from
                 * the outside (e.g. through animation or in the ParameterView) we have to update
                 * the CameraSetup and the interaction. */
                var shouldUpdateFromCamOp = _camSetupProvider.SelectedOperatorIsCamProvider && !CameraInteraction.SmoothedMovementInProgress;
                if (shouldUpdateFromCamOp)
                {
                    var newPos = _camSetupProvider.OperatorCameraProvider.GetLastPosition();
                    var newTarget = _camSetupProvider.OperatorCameraProvider.GetLastTarget();
                    CameraInteraction.SetCameraAfterExternalChanges(newPos, newTarget);
                    _camSetupProvider.ActiveCameraSetup.SetCameraAfterExternalChanges(newPos, newTarget);
                }
                return;
            }

            // Trigger update of other UI Elements like ParameterView
            // Because this will eventually triggere a RenderContent, we can skip rendering it here.
            if (_camSetupProvider.SelectedOperatorIsCamProvider)
            {
                App.Current.UpdateRequiredAfterUserInteraction = true;

            }

            RenderContent();
        }

        void App_UpdateAfterUserInteractionHandler(object sender, EventArgs e)
        {
            RenderContent();
        }




        #endregion


        #region Rendering
        private void SwitchToFullscreenMode()
        {
            var fsView = new FullScreenView(RenderConfiguration);
        }


        private void RenderContent()
        {
            if (!IsVisible || _renderSetup == null)
                return;

            if (TimeLoggingSourceEnabled)
                TimeLogger.BeginFrame(App.Current.Model.GlobalTime);

            _renderSetup.RenderContent();


            if (TimeLoggingSourceEnabled)
                TimeLogger.EndFrame();
        }


        private void SetupRenderer()
        {
            SetRendererSizeFromWindow();
            _renderSetup.SetupRendering();
            XSceneImage.Source = _renderSetup.D3DImageContainer;
        }





        private void SetRendererSizeFromWindow()
        {
            RenderConfiguration.Width = (int)XGrid.ActualWidth;
            RenderConfiguration.Height = (int)XGrid.ActualHeight;
        }




        /** After rendering a image this flag can be used to display UI-elements relevant for CubeMaps */
        public bool RenderedImageIsACubemap
        {
            get
            {
                return _renderSetup.RenderedImageIsACubemap;
            }
        }


        #endregion

        private ViewCameraSetupProvider _camSetupProvider;
        public D3DRenderSetup RenderSetup { get { return _renderSetup; } }
        private D3DRenderSetup _renderSetup;

        public RenderViewConfiguration RenderConfiguration { get; set; }
        public CameraInteraction CameraInteraction { get; set; }

        public bool TimeLoggingSourceEnabled { get; set; }

    }
}