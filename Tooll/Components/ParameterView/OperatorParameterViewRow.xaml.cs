// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Framefield.Core;
using Framefield.Core.Curve;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Framefield.Core.Commands;

namespace Framefield.Tooll
{

    public class ParameterGroupManipulatedEventArgs : System.EventArgs
    {
        public float Offset { get; private set; }

        public ParameterGroupManipulatedEventArgs(float offset)
        {
            Offset = offset;
        }
    }
    public delegate void ParameterGroupManipulatedDelegate(object o, ParameterGroupManipulatedEventArgs e);


    /// <summary>
    /// Interaction logic for OperatorParameterViewRow.xaml
    /// </summary>
    public partial class OperatorParameterViewRow : UserControl
    {
        private List<OperatorPart> m_OperatorParts;

        public OperatorParameterViewRow(List<OperatorPart> opParts)
        {
            m_OperatorParts = opParts;
            InitializeComponent();
            UpdateAnimationFocusHighlight();

            if (m_OperatorParts[0].Name.EndsWith("Trigger"))
            {
                var tiggerButton = new Button();
                tiggerButton.Content = "Trigger";
                XParameterValue.Children.Add(tiggerButton);
                Grid.SetColumn(tiggerButton, 1);
                XParameterValue.ColumnDefinitions[0].Width = new GridLength(0.2, GridUnitType.Star);
                XParameterValue.ColumnDefinitions[1].Width = new GridLength(0.8, GridUnitType.Star);

                tiggerButton.PreviewMouseDown += new MouseButtonEventHandler(tiggerButton_PreviewMouseDown);
                tiggerButton.PreviewMouseUp += new MouseButtonEventHandler(tiggerButton_PreviewMouseUp);
            }
        }

        private List<FloatParameterControl> GetFloatParameterControls()
        {
            List<FloatParameterControl> floatControls = new List<FloatParameterControl>();
            foreach (var fc in XParameterValue.Children)
            {
                var fpc = fc as FloatParameterControl;
                if (fpc != null)
                {
                    floatControls.Add(fpc);
                }
            }
            return floatControls;
        }

        private void tiggerButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var floatControls = GetFloatParameterControls();
            if (floatControls.Count == 1)
            {
                floatControls[0].StartManipulation();
                floatControls[0].ManipulateValue(1.0f);
                App.Current.UpdateRequiredAfterUserInteraction = true;
                floatControls[0].EndManipulation();
            }
            e.Handled = true;
        }

        private void tiggerButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var floatControls = GetFloatParameterControls();
            if (floatControls.Count == 1)
            {
                floatControls[0].StartManipulation();
                floatControls[0].ManipulateValue(0.0f);
                App.Current.UpdateRequiredAfterUserInteraction = true;
                floatControls[0].EndManipulation();
            }
            e.Handled = true;
        }



        private void ParameterViewRow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(ConnectionDragHelper.CONNECTION_LINE_OUTPUT_IDENTIFIER))
            {

                OperatorPart output = e.Data.GetData(ConnectionDragHelper.CONNECTION_LINE_OUTPUT_IDENTIFIER) as OperatorPart;
                OperatorPart input = null;

                // Note: For starters we only support single connections (no groups)
                if (XParameterValue.Children.Count > 0)
                {
                    var parameterControl = XParameterValue.Children[0] as IParameterControl;
                    if (parameterControl != null)
                    {
                        input = parameterControl.ValueHolder;
                    }
                }

                CreateAndAddConnection(input, output);
            }
        }

        private static void CreateAndAddConnection(OperatorPart input, OperatorPart output)
        {
            if (input == null || output == null)
                return;
            
            if (output.Type != input.Type) 
            {
                MessageBox.Show("You can not connect " + output.Type.ToString() + " to " + input.Type.ToString(),
                                "Sorry");
                return;
            }

            if (output.Parent == input.Parent) 
            {
                MessageBox.Show("You can not connect an operator to itself.",
                                "Sorry");
                return;
            }
            // check for cycle
            var parent = input.Parent;
            var outputsOfInputOp = parent.Outputs;
            var cc = new OperatorPart.CycleChecker(outputsOfInputOp);
            output.TraverseWithFunction(cc, null);
            if (cc.HasCycle)
            {
                MessageBox.Show("You can not connect " + output.Type.ToString() + " to " + input.Type.ToString(),
                                "As this would build a cycle, sorry");
                return;
            }


            var newConnection = new Connection(output.Parent, output, input.Parent, input, input.IsMultiInput ? input.Connections.Count : 0);

            if (input.IsMultiInput || input.Connections.Count == 0)
            {
                App.Current.MainWindow.InsertConnectionAt(newConnection);
            }
            else
            {
                App.Current.MainWindow.ReplaceConnectionAt(newConnection);
            }
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        private void ParameterViewRow_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(ConnectionDragHelper.CONNECTION_LINE_OUTPUT_IDENTIFIER) || sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private List<ICurve> GetAnimationCurves()
        {
            List<ICurve> animationCurves = new List<ICurve>();
            foreach (var opPart in m_OperatorParts)
            {
                OperatorPart animationOpPart = Animation.GetRegardingAnimationOpPart(opPart);
                if (animationOpPart != null)
                {
                    animationCurves.Add(animationOpPart.Func as ICurve);
                }
            }
            return animationCurves;
        }


        public void UpdateAnimationFocusHighlight()
        {
            var animationCurves = GetAnimationCurves();
            bool isHightlighted = false;

            if (animationCurves.Count > 0)
            {
                if (App.Current.MainWindow.CompositionView.XTimeView.XAnimationCurveEditor.FocusedCurves.Count == 0)
                {
                    isHightlighted = true;
                }
                else
                {
                    foreach (var c in animationCurves)
                    {
                        if (
                            App.Current.MainWindow.CompositionView.XTimeView.XAnimationCurveEditor.FocusedCurves
                               .Contains(c))
                        {
                            isHightlighted = true;
                            break;
                        }
                    }
                }
            }
            XParameterNameButton.Foreground = isHightlighted
                                                    ? Brushes.Orange
                                                    : Brushes.White;
        }

        public event EventHandler<System.EventArgs> ResetToDefaultEvent;
        public event EventHandler<System.EventArgs> StartManipulationEvent;
        public event ParameterGroupManipulatedDelegate UpdateManipulationEvent;
        public event EventHandler<System.EventArgs> EndManipulationEvent;


        private bool _mousePressedOnButton;
        private bool _dragHasModifiedValue;
        private System.Windows.Point _mouseRefPosition;

        private void MouseLeftButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            var el = sender as UIElement;
            if (el != null)
            {
                el.CaptureMouse();
                _mousePressedOnButton = true;
                _dragHasModifiedValue = false;
                _mouseRefPosition = e.GetPosition(this);
            }
            e.Handled = true;            
        }

        protected void MouseMoveHandler(object sender, MouseEventArgs e)
        {
            if (_mousePressedOnButton)
            {
                var diff = e.GetPosition(this).X - _mouseRefPosition.X;
                if (!_dragHasModifiedValue && Math.Abs(diff) > SystemParameters.MinimumHorizontalDragDistance)
                {
                    if (StartManipulationEvent != null)
                    {
                        _mouseRefPosition = e.GetPosition(this);
                        diff = 0;
                        StartManipulationEvent(this, EventArgs.Empty);
                    }
                    _dragHasModifiedValue = true;
                }

                if (_dragHasModifiedValue)
                {
                    if (UpdateManipulationEvent != null)
                    {
                        var offset = (float) (diff*UIHelper.SubScaleFromKeyboardModifiers());
                        UpdateManipulationEvent(this, new ParameterGroupManipulatedEventArgs(offset));
                        
                    }
                }
            }
            e.Handled = true;
        }

        private void MouseLeftButtonUpHandler(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(null);
            _mousePressedOnButton = false;
            if (!_dragHasModifiedValue)
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (ResetToDefaultEvent != null)
                    {
                        ResetToDefaultEvent(this, EventArgs.Empty);
                    }
                }
                else
                {
                    SetAnimationCurveFocus();
                }
            }
            else
            {
                if (EndManipulationEvent != null)
                {
                    EndManipulationEvent(this, EventArgs.Empty);
                }                
            }

            _dragHasModifiedValue = false;
            e.Handled = true;
        }

        #region dirty stuff
        private OperatorParameterView m_OPV;
        public OperatorParameterView OPV {
            get {
                if (m_OPV == null)
                    m_OPV = UIHelper.FindParent<OperatorParameterView>(this);
                return m_OPV;
            }
        }
        #endregion


        private void SetAnimationCurveFocus()
        {
            var focusedCurves = App.Current.MainWindow.CompositionView.XTimeView.XAnimationCurveEditor.FocusedCurves;

            //if (GetAnimationCurves().Count > 0) {
            //    if (Keyboard.Modifiers != ModifierKeys.Control) {
            //        focusedCurves.Clear();
            //    }
            //}

            foreach (var curve in GetAnimationCurves())
            {
                if (focusedCurves.Contains(curve))
                {
                    focusedCurves.Remove(curve);
                }
                else
                {
                    focusedCurves.Add(curve);
                }
            }
            OPV.UpdateParameterViewRowHighlights();

        }
    }


}
