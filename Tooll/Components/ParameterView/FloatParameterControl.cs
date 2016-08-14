// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

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
using Framefield.Core;
using Framefield.Core.Commands;
using Framefield.Core.Curve;
using System.Diagnostics;

namespace Framefield.Tooll
{
    /// <summary>
    /// This Components binds a Float-Parameter with a FloatEdit control and a VirtualSlider overlay.
    /// To learn more about the nesting structure, have a look at the following diagram: http://streber.pixtur.de/5271
    ///
    /// It is automatically being constructred in OperatorParameterView and only works for SingleInputs.
    /// 
    /// At the moment this structure seems awkward and too complicate. We should refactor the following things:
    /// - The updating events should refactored to bindings
    /// - CurrentValue should be removed from Slider Indicator and FloatParameterControl and bound via MVVM to an operatorViewModel
    /// - It's unclear why this component needs to register to GlobalTimeChangedEvent
    /// 
    /// </summary>
    public partial class FloatParameterControl : FloatEditButton, IParameterControl
    {
        public OperatorPart ValueHolder { get; private set; }

        public FloatParameterControl(OperatorPart valueHolder)
            : base()
        {
            ValueHolder = valueHolder;

            _metaInput = valueHolder.Parent.GetMetaInput(valueHolder);
            if (_metaInput != null)
            {
                Scale = _metaInput.Scale;
                Min = _metaInput.Min;
                Max = _metaInput.Max;
                var floatDefault = _metaInput.DefaultValue as Float;
                if (floatDefault != null)
                {
                    Default = floatDefault.Val;
                }
            }

            UpdateAnimationReference();
            UpdateGUI();

            Loaded += FloatParameterControl_LoadedHandler;
        }

        #region XAML handlers (load/unloead/context-menu, etc.)
        //===================================================================================
        // XAML Event Handlers

        void FloatParameterControl_LoadedHandler(object sender, RoutedEventArgs e)
        {
            // Connect Operator Part and Global Time
            ValueHolder.ManipulatedEvent += ValueHolder_ManipulatedHandler;
            ValueHolder.ChangedEvent += ValueHolder_ChangedHandler;
            App.Current.Model.GlobalTimeChangedEvent += ValueHolder_ChangedHandler;

            // Events from FloatEditButton
            ValueChangedEvent += FloatEditButton_ValueChangedHandler;
            ResetToDefaultEvent += FloatEditButton_ResetToDefaultHandler;
            EditingStartedEvent += FloatEditButton_EditingStartedHandler;
            EditingEndedEvent += FloatEditButton_EditingEndedHandler;
            EditingCancelledEvent += FloatEditButton_CancelledHandler;

            // Add additional XAML Events
            PreviewKeyDown += FloatParameterControl_PreviewKeyDownHandler;
            Drop += Grid_DropHandler;
            MouseRightButtonUp += MouseRightButtonUpHandler;

            Unloaded += UnloadHandler;
        }


        private void UnloadHandler(object sender, RoutedEventArgs e)
        {
            // Connect Operator Part and Global Time
            ValueHolder.ManipulatedEvent -= ValueHolder_ManipulatedHandler;   // WTF? what's the difference between changed and modified?
            ValueHolder.ChangedEvent -= ValueHolder_ChangedHandler;
            App.Current.Model.GlobalTimeChangedEvent -= ValueHolder_ChangedHandler;

            // Events from FloatEditButton
            ValueChangedEvent -= FloatEditButton_ValueChangedHandler;
            ResetToDefaultEvent -= FloatEditButton_ResetToDefaultHandler;
            EditingStartedEvent -= FloatEditButton_EditingStartedHandler;
            EditingEndedEvent -= FloatEditButton_EditingEndedHandler;
            EditingCancelledEvent -= FloatEditButton_CancelledHandler;

            // Add additional XAML Events
            PreviewKeyDown -= FloatParameterControl_PreviewKeyDownHandler;
            Drop -= Grid_DropHandler;
            MouseRightButtonUp -= MouseRightButtonUpHandler;
        }


        // Implement TAB-Movement
        void FloatParameterControl_PreviewKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                if (TabMoveEvent != null)
                {
                    TabMoveEvent(this, Keyboard.Modifiers == ModifierKeys.Shift);
                    e.Handled = true;
                }
            }
        }

        public delegate void TabMoveDelegate(FloatParameterControl sender, bool backwards);
        public event TabMoveDelegate TabMoveEvent;

        // Show Context Menu
        private void MouseRightButtonUpHandler(object sender, MouseButtonEventArgs e)
        {
            var userControl = sender as Control;
            if (userControl == null)
                return;

            if (IsConnected)
            {
                userControl.ContextMenu = null;
                return;
            }

            var contextMenu = new ContextMenu();

            // Remove Animation
            if (IsAnimated)
            {
                var item = new MenuItem();
                item.Header = "Remove Animation";
                item.Click += (o, a) => {
                    ExecuteRemoveAnimation();
                };
                contextMenu.Items.Add(item);
            }

            // Add Animation
            if (!IsAnimated)
            {
                var item = new MenuItem();
                item.Header = "Animate";
                item.Click += (o, a) => {
                    App.Current.UndoRedoStack.AddAndExecute(new SetupAnimationCommand(ValueHolder, App.Current.Model.GlobalTime));
                };
                contextMenu.Items.Add(item);
            }
            contextMenu.Items.Add(new Separator());

            // Reset to Default (Ctrl-Click)            
            {
                var menuItem = new MenuItem() { Header = "Reset" };
                menuItem.Click += (o, a) => {
                                    TryExecutatingResetSingleParameter();
                                };
                menuItem.InputGestureText = "CTRL+Click";
                menuItem.IsEnabled = !ValueHolder.IsDefaultFuncSet;
                contextMenu.Items.Add(menuItem);
            }

            // Set as Default Value            
            {
                var menuItem = new MenuItem() { Header = "Set as Default" };
                menuItem.Click += (o, a) => {
                    TryExecutingSetSingleParamAsDefault();
                };
                menuItem.IsEnabled = !ValueHolder.IsDefaultFuncSet;
                contextMenu.Items.Add(menuItem);
            }
            userControl.ContextMenu = contextMenu;
        }


        private void Grid_DropHandler(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(ConnectionDragHelper.CONNECTION_LINE_OUTPUT_IDENTIFIER)) 
                return;
            
            var output = e.Data.GetData(ConnectionDragHelper.CONNECTION_LINE_OUTPUT_IDENTIFIER) as Core.OperatorPart;
            if (output == null) 
                return;

            if (output.Type != ValueHolder.Type)
            {
                MessageBox.Show("You can not connect " + output.Type.ToString() + " to " + ValueHolder.Type.ToString(), "Sorry");
                return;
            }
            if (output.Parent == ValueHolder.Parent)
            {
                MessageBox.Show("You can not connect an operator to itself.", "Sorry");
                return;
            }

            var newConnection = new Connection(output.Parent, output, ValueHolder.Parent, ValueHolder, ValueHolder.IsMultiInput ? ValueHolder.Connections.Count : 0);

            if (ValueHolder.IsMultiInput || ValueHolder.Connections.Count == 0)
            {
                App.Current.MainWindow.InsertConnectionAt(newConnection);
            }
            else
            {
                App.Current.MainWindow.ReplaceConnectionAt(newConnection);
            }
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }
        #endregion


        #region handlers for FloatEditButton Events
        //===================================================================================
        // FloatEditButton Handlers

        private AddOrUpdateKeyframeCommand _addKeyframeCommand;
        private UpdateOperatorPartValueFunctionCommand _updateValueCommand;

        private void FloatEditButton_EditingStartedHandler()
        {
            if (IsAnimated)
            {
                _addKeyframeCommand = new AddOrUpdateKeyframeCommand(App.Current.Model.GlobalTime, Value, ValueHolder);
            }
            else
            {
                // Disable rebuilding of animation curves...
                App.Current.MainWindow.CompositionView.XTimeView.XAnimationCurveEditor.DisableCurveUpdatesOnModifiedEvent = true;

                _updateValueCommand = new UpdateOperatorPartValueFunctionCommand(ValueHolder, new Float(Value));
            }
        }

        private void FloatEditButton_ValueChangedHandler(float newValue)
        {
            newValue = Math.Max(_metaInput.Min, Math.Min(_metaInput.Max, newValue));  // ToDo: clamping should be done by FloatEdit

            // Update animation curve
            if (IsAnimated && _addKeyframeCommand != null)
            {
                _addKeyframeCommand.KeyframeValue.Value = newValue;
                _addKeyframeCommand.Do();
            }
            else if (_updateValueCommand != null)
            {
                _updateValueCommand.Value = new Float(newValue);
                _updateValueCommand.Do();
            }
            App.Current.UpdateRequiredAfterUserInteraction = true; // ToDo: This line should be moved to the source of interaction. Here, we only deal with result.
        }

        private void FloatEditButton_EditingEndedHandler()
        {
            if (IsAnimated)
            {
                App.Current.UndoRedoStack.AddAndExecute(_addKeyframeCommand);
                _addKeyframeCommand = null;
            }
            else
            {
                // Re-enable rebuilding of animation curves...
                App.Current.MainWindow.CompositionView.XTimeView.XAnimationCurveEditor.DisableCurveUpdatesOnModifiedEvent = false;

                if (_updateValueCommand != null && ((_updateValueCommand.Value as Float).Val) != ((_updateValueCommand.PreviousValue as Float).Val))
                    App.Current.UndoRedoStack.Add(_updateValueCommand);

                _updateValueCommand = null;
            }
        }

        private void FloatEditButton_CancelledHandler()
        {
            _addKeyframeCommand = null;
            _updateValueCommand = null;
        }

        private void FloatEditButton_ResetToDefaultHandler()
        {
            TryExecutatingResetSingleParameter();
        }
        #endregion


        #region manipulation from parameter name
        /*===================================================================================
         * Parameter Row Handlers (e.g. Clicking and Dragging the parameter name)
         *
         * This is events are triggered by the XParameterName Button in OperatorParameterViewRow, 
         * which emits events for reset and value manipulation.
         * This binding between these events and the following handlers is done in Constructor of 
         * the OperatorParameterView
         */
        public void ParameterRow_ResetSingleParameterHandler(object sender, EventArgs e)
        {
            TryExecutatingResetSingleParameter();
        }

        private UpdateOperatorPartValueFunctionCommand _setValueCommand;
        float _keepValueBeforeManipulation;

        public void ParameterRow_StartManipulationHandler(object sender, EventArgs e)
        {
            _keepValueBeforeManipulation = Value;
            var floatObject = ((ValueHolder.Func as Core.Utilities.ValueFunction).Value as Float);
            if (floatObject != null)
            {
                _setValueCommand = new UpdateOperatorPartValueFunctionCommand(ValueHolder, floatObject);
            }
        }

        public void ParameterRow_UpdateManipulaitonHandler(object sender, ParameterGroupManipulatedEventArgs e)
        {
            var factor = (float)(e.Offset / 100.0 * UIHelper.SubScaleFromKeyboardModifiers() + 1);
            var metaInput = ValueHolder.Parent.GetMetaInput(ValueHolder);
            var newValue = _keepValueBeforeManipulation * (1 + factor * metaInput.Scale);
            newValue = Core.Utilities.Clamp(newValue, metaInput.Min, metaInput.Max);
            _setValueCommand.Value = new Core.Float(newValue);
            _setValueCommand.Do();
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        public void ParameterRow_EndManipulationHandler(object sender, EventArgs e)
        {
            App.Current.UndoRedoStack.Add(_setValueCommand);

            _setValueCommand = null;
        }
        #endregion


        # region implementing of execution of functions required for serveral event handlers
        //===================================================================================

        /* This can be triggered from context-menu or CTRL-clicking.
         * It selected the valid command depending on context:
         * - If animated -> Remove Animation and set to default
         * - If connected -> Pop up dialog
         * - else: Set to default.
         * 
         * A similar method exist for parameter groups which implements
         * the same as a macrocommand.
         */
        private void TryExecutatingResetSingleParameter()
        {
            if (IsConnected)
            {
                MessageBox.Show("To reset a connected paramter, you first have to disconnect it.");
                return;
            }
             
            var removeAnimationCommand = new RemoveAnimationCommand(ValueHolder,0);
            var setValueToDefaultCmd = new ResetInputToDefault(ValueHolder);
            Framefield.Core.ICommand[] commands = { removeAnimationCommand, setValueToDefaultCmd };

            App.Current.UndoRedoStack.AddAndExecute(new MacroCommand("Reset Paramter", commands));
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        private void ExecuteRemoveAnimation()
        {
            var lastValue = Core.Curve.Utils.GetCurrentValueAtTime(ValueHolder, App.Current.Model.GlobalTime);
            App.Current.UndoRedoStack.AddAndExecute(new RemoveAnimationCommand(ValueHolder, lastValue));
        }

        private void TryExecutingSetSingleParamAsDefault()
        {
            var setInputAsAndResetToDefaultCmd = new SetInputAsAndResetToDefaultCommand(ValueHolder);
            App.Current.UndoRedoStack.AddAndExecute(setInputAsAndResetToDefaultCmd);
            App.Current.UpdateRequiredAfterUserInteraction = true; 
        }
        #endregion


        #region update-required events from ValueHolder
        //===================================================================================
        // Various events to trigger repaints / refreshing UI

        private void ValueHolder_ManipulatedHandler(object sender, EventArgs args)
        {
            UpdateAnimationReference();
            UpdateGUI();
        }

        private void ValueHolder_ChangedHandler(object sender, EventArgs e)
        {
            UpdateGUI();
        }
        #endregion


        #region update user interface
        //-----------------------------------------------------------------------------------
        // Helper Functions to update UI

        private void UpdateGUI()
        {
            float newValue = Value;
            if (IsAnimated)
            {
                newValue = (float)_animationCurve.GetSampledValue((double)App.Current.Model.GlobalTime);
            }
            else if (IsConnected)
            {
                newValue = ValueHolder.Eval(new Core.OperatorPartContext()).Value;                
            }
            else
            {
                var func = ValueHolder.Func as Utilities.ValueFunction;
                if (func == null)
                {
                    Logger.Error("FloatParameterControl has invalid value func");
                    return;
                }
                var floatValue = func.Value as Float;
                if (floatValue != null)
                    newValue = floatValue.Val;
            }

            // Check to prevent update events on reassigment
            if (newValue != Value)
                Value = newValue;

            if (IsAnimated) {
                Foreground = Brushes.Orange;
            }
            else if (ValueHolder.Connections.Count > 0)
            {
                Foreground = Brushes.DodgerBlue;
            }

            else if (ValueHolder.IsDefaultFuncSet)
            {
                var b = Brushes.White.Clone();
                b.Opacity = 0.3;
                Foreground = b;
            }
            else
            {
                Foreground = Brushes.White;
           }
        }
        #endregion

        //---------------------------------------------------------------------------------
        // Members and helper functions

        private void UpdateAnimationReference()
        {
            OperatorPart animationOpPart = Animation.GetRegardingAnimationOpPart(ValueHolder);
            if (animationOpPart != null)
            {
                _animationCurve = animationOpPart.Func as ICurve;
            }

            else
            {
                _animationCurve = null;
            }
        }

        private MetaInput _metaInput;
        private ICurve _animationCurve = null;
        private const float VISIBLE_SLIDER_RANGE = 100.0f;

        public bool IsAnimated
        {
            get { return _animationCurve != null; }
        }

        public override bool IsLocked()
        {
            return IsConnected;
        }

        private bool IsConnected {
            get { return _animationCurve == null && ValueHolder.Connections.Count > 0; }
        }
    }
}

