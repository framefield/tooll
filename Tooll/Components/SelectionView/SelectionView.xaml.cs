// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Framefield.Core.Curve;
using Framefield.Core;
using System.Linq;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Framefield.Tooll.Components.SelectionView
{

    public partial class SelectionView : UserControl
    {
        public SelectionView()
        {
            InitializeComponent();

            DisplayMode = DisplayAs.Nothing;
        }

        private void XRenderView_Loaded(object sender, RoutedEventArgs e)
        {
            ContextMenuOpening += ContextMenuOpening_Handler;
            var CGV = App.Current.MainWindow.CompositionView.XCompositionGraphView;
            CGV.OperatorHoverStartEvent += CGV_OperatorHoverStartEvent;
            CGV.OperatorHoverEndEvent += CGV_OperatorHoverEndEvent;

        }


        private void XRenderView_Unloaded(object sender, RoutedEventArgs e)
        {
            ContextMenuOpening -= ContextMenuOpening_Handler;
        }



        #region hover event handling
        private void CGV_OperatorHoverStartEvent(object sender, OperatorWidget.HoverEventsArgs e)
        {
            if (XHoverButton.IsChecked == false)
                return;

            if (e.OpWidget == _shownOperatorWidget)
            {
                return;
            }

            if (_isShowingHover)
                return;

            _selectionBeforeHover = _shownOperatorWidget;
            SetOperatorWidget(e.OpWidget, forHoverOnly: true);
            _isShowingHover = true;
        }

        private void CGV_OperatorHoverEndEvent(object sender, OperatorWidget.HoverEventsArgs e)
        {
            if (XHoverButton.IsChecked == false)
                return;

            if (!_isShowingHover)
                return;

            SetOperatorWidget(_selectionBeforeHover);
            _isShowingHover = false;
            _selectionBeforeHover = null;
        }

        bool _isShowingHover = false;
        private OperatorWidget _selectionBeforeHover;
        #endregion




        private void ContextMenuOpening_Handler(object sender, ContextMenuEventArgs e)
        {
            if (Operator == null || Operator.Outputs.Count < 1 || _displayMode == DisplayAs.Curve)
                return;

            if (ContextMenu == null)
            {
                ContextMenu = new ContextMenu();
            }

            ContextMenu.Items.Clear();
            int outputIdx = 0;
            foreach (var output in Operator.Outputs)
            {

                var entryText = string.Format("Show Output: {0} ({1})", output.Name, outputIdx + 1);
                var menuItem = new MenuItem { Header = entryText, IsChecked = _shownOutputIndex == outputIdx };
                var outputIdxClosure = outputIdx;
                menuItem.Click += (o, args) => SelectShownOutput(outputIdxClosure);
                ContextMenu.Items.Add(menuItem);

                // Add sub menu for cube-map image selection
                if (output.Type == FunctionType.Image)
                {
                    if (XShowContentControl.RenderedImageIsACubemap)
                    {
                        for (var cubeMapSideIndex = 0; cubeMapSideIndex < 6; ++cubeMapSideIndex)
                        {
                            var cubeMapSelectionItem = new MenuItem()
                            {
                                Header = "Cube Map Side " + (cubeMapSideIndex + 1),
                                IsChecked = XShowContentControl.RenderConfiguration.PreferredCubeMapSideIndex == cubeMapSideIndex
                            };
                            var cubeMapSideIndexClosure = cubeMapSideIndex;
                            menuItem.Items.Add(cubeMapSelectionItem);
                            cubeMapSelectionItem.Click +=
                                (o, args) =>
                                {
                                    XShowContentControl.RenderConfiguration.PreferredCubeMapSideIndex = cubeMapSideIndexClosure;
                                };
                        }

                        // Entry for rendering as sphere
                        var cubeMapAsSphereItem = new MenuItem()
                        {
                            Header = "Cube Map as Sphere ",
                            IsChecked = XShowContentControl.RenderConfiguration.PreferredCubeMapSideIndex == CUBE_MAP_AS_SPHERE_SIDE
                        };
                        menuItem.Items.Add(cubeMapAsSphereItem);
                        cubeMapAsSphereItem.Click +=
                            (o, args) =>
                            {
                                XShowContentControl.RenderConfiguration.PreferredCubeMapSideIndex = CUBE_MAP_AS_SPHERE_SIDE;
                            };
                    }
                }
                outputIdx++;
            }
            ContextMenu.IsOpen = true;
        }

        private const int CUBE_MAP_AS_SPHERE_SIDE = 6;

        public bool TimeLoggingSourceEnabled
        {
            get { return XShowContentControl.TimeLoggingSourceEnabled; }
            set { XShowContentControl.TimeLoggingSourceEnabled = value; }
        }


        private void SelectShownOutput(int outputIndex)
        {
            if (Operator == null || Operator.Outputs.Count < outputIndex + 1)
                return;

            _shownOutputIndex = outputIndex;
            UpdateShowControls();
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }


        enum DisplayAs
        {
            Scene,
            Image,
            Text,
            Curve,
            Nothing,
        }

        private DisplayAs _displayMode;
        private DisplayAs DisplayMode
        {
            get { return _displayMode; }
            set
            {
                var needsUpdate = (value != _displayMode);
                _displayMode = value;

                if (needsUpdate)
                    UpdateControlVisibility();
            }
        }


        private void UpdateControlVisibility()
        {
            var visibilityOfCurve = Visibility.Hidden;
            var visibilityOfScene = Visibility.Hidden;
            var visibilityOfText = Visibility.Hidden;

            switch (DisplayMode)
            {
                case DisplayAs.Curve:
                    visibilityOfCurve = Visibility.Visible;
                    break;
                case DisplayAs.Scene:
                    visibilityOfScene = Visibility.Visible;
                    break;
                //case DisplayAs.Image:
                //    visibilityOfImage = Visibility.Visible;
                //    break;
                case DisplayAs.Text:
                    visibilityOfText = Visibility.Visible;
                    break;
            }

            XShowCurveControl.Visibility = visibilityOfCurve;
            XShowContentControl.Visibility = visibilityOfScene;
            XShowAsTextControl.Visibility = visibilityOfText;
            //XShowImageControl.Visibility = visibilityImage;
        }

        private void DiscardShownOperator()
        {
            DisplayMode = DisplayAs.Nothing;
            if (_shownOperatorWidget != null)
            {
                _shownOperatorWidget.Operator.Parent.OperatorRemovedEvent -= Parent_OperatorRemovedEventHandler;
            }

            _shownOperatorWidget = null;
            XSelectedOperatorName.Text = "?";
            XLockedButton.IsChecked = false;

            XShowCurveControl.Curve = null;
            XShowAsTextControl.SetOperatorAndOutput(null);
            XShowContentControl.SetOperatorAndOutputIndex(null);
        }

        public Operator Operator
        {
            get
            {
                return _shownOperatorWidget == null ? null
                                                    : _shownOperatorWidget.Operator;
            }
        }

        private int _shownOutputIndex = 0;


        /** Tries to select operator widget if view isn't locked. 
         Returns true if successful. */
        public bool SetOperatorWidget(OperatorWidget opWidget, bool forHoverOnly = false)
        {
            if (XLockedButton.IsChecked == true)
                return false;

            if (!forHoverOnly)
            {
                _isShowingHover = false;
            }


            if (opWidget == null || opWidget == _shownOperatorWidget)
                return false;

            var op = opWidget.Operator;
            //if (op == null)
            //    return false;

            // Was explicity set e.g. by selection

            //if (forHoverOnly)
            //{
            //    _selectionBeforeHover = opWidget;
            //}
            //else
            //{
            //    _selectionBeforeHover = null;
            //}

            // Remove handler from old op?  Why do we need this?
            if (_shownOperatorWidget != null)
            {
                _shownOperatorWidget.Operator.Parent.OperatorRemovedEvent -= Parent_OperatorRemovedEventHandler;
            }

            _shownOperatorWidget = opWidget;
            op.Parent.OperatorRemovedEvent += Parent_OperatorRemovedEventHandler;

            XSelectedOperatorName.Text = op.Definition.Name + "   " + op.Name;

            _shownOutputIndex = 0;

            UpdateShowControls();
            return true;
        }



        private void UpdateShowControls()
        {
            var newDisplayMode = DisplayAs.Nothing;

            if (!Operator.Outputs.Any() || Operator.Outputs.Count < _shownOutputIndex)
            {
                DisplayMode = DisplayAs.Nothing;
                return;
            }
            var output = Operator.Outputs[_shownOutputIndex];

            if (Operator.InternalParts.Count > 0 && Operator.InternalParts[0].Func is ICurve)
            {
                var curve = Operator.InternalParts[0].Func as ICurve;
                XShowCurveControl.Curve = curve;
                newDisplayMode = DisplayAs.Curve;
            }
            else if (output.Type == FunctionType.Image || output.Type == FunctionType.Scene || output.Type == FunctionType.Mesh)
            {
                newDisplayMode = DisplayAs.Scene;
                XShowContentControl.SetOperatorAndOutputIndex(Operator, _shownOutputIndex);
            }
            else if (output.Type == FunctionType.Float || output.Type == FunctionType.Generic ||
                     output.Type == FunctionType.Text)
            {
                newDisplayMode = DisplayAs.Text;
                XShowAsTextControl.SetOperatorAndOutput(Operator, _shownOutputIndex);
            }
            DisplayMode = newDisplayMode;
        }

        private void Parent_OperatorRemovedEventHandler(object obj, OperatorChangedEventArgs args)
        {
            if (_shownOperatorWidget != null && args.Operator == _shownOperatorWidget.Operator)
            {
                DiscardShownOperator();
            }
        }


        public void UpdateViewToCurrentSelectionHandler(object sender, SelectionHandler.FirstSelectedChangedEventArgs e)
        {
            SetOperatorWidget(e.Element as OperatorWidget);
        }


        private void ToggleLocked_ClickedHandler(object sender, RoutedEventArgs e)
        #region XAML Event handlers
        {
            if (XLockedButton.IsChecked == true)
            {
                if (_shownOperatorWidget != null)
                {
                    _shownOperatorWidget.StickyCount++;
                }
            }
            else
            {
                if (_shownOperatorWidget != null)
                {
                    _shownOperatorWidget.StickyCount--;
                }

                var cgv = App.Current.MainWindow.CompositionView.CompositionGraphView;
                if (cgv.SelectedElements.Any())
                {
                    var ow = cgv.SelectedElements[0] as OperatorWidget;
                    SetOperatorWidget(ow);
                }
            }
        }

        private void GotFocusHandler(object sender, RoutedEventArgs e)
        {
            this.BorderThickness = new Thickness(1);
            this.BorderBrush = Brushes.DarkGray;
        }

        private void LostFocusHandler(object sender, RoutedEventArgs e)
        {
            this.BorderThickness = new Thickness(1);
            this.BorderBrush = Brushes.Black;
        }

        private void XGizmoButton_Click(object sender, RoutedEventArgs e)
        {
            XShowContentControl.RenderConfiguration.ShowGridAndGizmos = (XGizmoButton.IsChecked == true);

            if (_shownOperatorWidget == null)
                return;

            // FixMe: A more efficient implementation should use InvalidateVariableAccessors()
            var invalidator = new Framefield.Core.OperatorPart.InvalidateInvalidatables();
            _shownOperatorWidget.Operator.Outputs[0].TraverseWithFunctionUseSpecificBehavior(null, invalidator);

            App.Current.UpdateRequiredAfterUserInteraction = true;
        }


        private void XLinearButton_Click(object sender, RoutedEventArgs e)
        {
            XShowContentControl.RenderConfiguration.RenderWithGammaCorrection = (XLinearButton.IsChecked == true);

            if (_shownOperatorWidget == null)
                return;

            // FixMe: A more efficient implementation should use InvalidateVariableAccessors()
            var invalidator = new Framefield.Core.OperatorPart.InvalidateInvalidatables();
            _shownOperatorWidget.Operator.Outputs[0].TraverseWithFunctionUseSpecificBehavior(null, invalidator);

            App.Current.UpdateRequiredAfterUserInteraction = true;
        }
        #endregion


        private OperatorWidget _shownOperatorWidget;
        private void OnClickLockedOpLabel(object sender, MouseButtonEventArgs e)
        {
            if (_shownOperatorWidget != null)
            {
                var cgv = App.Current.MainWindow.CompositionView.CompositionGraphView;
                cgv.SelectionHandler.SetElement(_shownOperatorWidget);
                cgv.CenterAllOrSelectedElements();
            }
        }

        public int PreferredCubeMapSideIndex { get; set; }


    }
}
