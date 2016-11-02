// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

﻿using Framefield.Core.Curve;
using Framefield.Core;
using System.Linq;
﻿using System.Windows.Input;
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
            ContextMenuOpening += ContextMenuOpening_Handler;
        }


        void ContextMenuOpening_Handler(object sender, ContextMenuEventArgs e)
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
                    var description = XShowSceneControl.RenderSetup.lastRenderedImageDescription;
                    if (description.HasValue)
                    {
                        if (description.Value.ArraySize > 1)
                        {
                            for (var cubeMapSideIndex = 0;
                                cubeMapSideIndex < description.Value.ArraySize;
                                ++cubeMapSideIndex)
                            {
                                var cubeMapSelectionItem = new MenuItem()
                                {
                                    Header = "Cube Map Side " + (cubeMapSideIndex + 1),
                                    IsChecked = XShowSceneControl.RenderSetup.PreferredCubeMapSideIndex == cubeMapSideIndex
                                };
                                var cubeMapSideIndexClosure = cubeMapSideIndex;
                                menuItem.Items.Add(cubeMapSelectionItem);
                                cubeMapSelectionItem.Click +=
                                    (o, args) =>
                                    {
                                        XShowSceneControl.RenderSetup.PreferredCubeMapSideIndex =
                                            cubeMapSideIndexClosure;
                                    };
                            }
                        }
                    }
                }

                outputIdx++;
            }
            ContextMenu.IsOpen = true;            
        }

        public bool TimeLoggingSourceEnabled
        {
            get { return XShowSceneControl.TimeLoggingSourceEnabled; }
            set { XShowSceneControl.TimeLoggingSourceEnabled = value; }
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

                if(needsUpdate)
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
            XShowSceneControl.Visibility = visibilityOfScene;
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
            XStickyCheckbox.IsChecked = false;

            XShowCurveControl.Curve = null;
            XShowAsTextControl.SetOperatorAndOutput(null);
            XShowSceneControl.SetOperatorAndOutputIndex(null);
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

        public void SetOperatorWidget(OperatorWidget opWidget) 
        {
            if (opWidget == null || opWidget == _shownOperatorWidget)
                return;

            var op = opWidget.Operator;
            if (op == null)
                return;

            if (XStickyCheckbox.IsChecked == true)
                return;    

            // Remove handler from old op?
            if (_shownOperatorWidget != null)
            {
                _shownOperatorWidget.Operator.Parent.OperatorRemovedEvent -= Parent_OperatorRemovedEventHandler;
            }

            _shownOperatorWidget = opWidget;
            op.Parent.OperatorRemovedEvent += Parent_OperatorRemovedEventHandler;

            XSelectedOperatorName.Text = op.Definition.Name + "   " + op.Name;

            _shownOutputIndex = 0;
            
            UpdateShowControls();
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
                XShowSceneControl.SetOperatorAndOutputIndex(Operator, _shownOutputIndex);
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

        private void CheckBox_StickyClickedHandler(object sender, RoutedEventArgs e)
        #region XAML Event handlers
        {
            if (XStickyCheckbox.IsChecked == true)
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
            XShowSceneControl.ShowGridAndGizmos = (XGizmoButton.IsChecked == true);

            if (_shownOperatorWidget == null)
                return;

            // FixMe: A more efficient implementation should use InvalidateVariableAccessors()
            var invalidator = new Framefield.Core.OperatorPart.InvalidateInvalidatables();
            _shownOperatorWidget.Operator.Outputs[0].TraverseWithFunctionUseSpecificBehavior(null, invalidator);

            App.Current.UpdateRequiredAfterUserInteraction = true;
        }
        private void XLinearButton_Click(object sender, RoutedEventArgs e)
        {
            XShowSceneControl.RenderWithGammaCorrection = (XLinearButton.IsChecked == true);

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
                cgv.CenterSelectedElements();
            }
        }
    }
}
