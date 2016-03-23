// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Framefield.Core;
using Framefield.Core.Commands;
using Framefield.Core.Curve;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for Enum Parameter
    /// </summary>
    public partial class EnumParameterValue : UserControl, IParameterControl
    {
        public OperatorPart ValueHolder { get; private set; }


        public EnumParameterValue(OperatorPart input)
        {
            ValueHolder = input;

            InitializeComponent();

            _metaInput = input.Parent.GetMetaInput(input);
            var enumValues = _metaInput.EnumValues;

            foreach (var enumEntry in enumValues)
            {
                XComboBox.Items.Add(enumEntry.Name);
            }

            XComboBox.SelectionChanged += (o, e) =>
            {
                if (!_changeEventsEnabled)
                    return;

                var idx = XComboBox.SelectedIndex;
                var value = new Float((float)enumValues[idx].Value);
                var _setValueCommand = new UpdateOperatorPartValueFunctionCommand(input, value);
                App.Current.UndoRedoStack.AddAndExecute(_setValueCommand);
                App.Current.UpdateRequiredAfterUserInteraction = true;
            };

            UpdateGUI();
            ValueHolder.ManipulatedEvent += ValueHolder_ManipulatedHandler;
            ValueHolder.ChangedEvent += ValueHolder_ChangedHandler;
            Unloaded += UnloadHandler;
        }

        private void UnloadHandler(object sender, RoutedEventArgs e)
        {
            ValueHolder.ManipulatedEvent -= ValueHolder_ManipulatedHandler;
            ValueHolder.ChangedEvent -= ValueHolder_ChangedHandler;
        }


        public void ResetToDefaultHandler(object sender, EventArgs e)
        {
            TryExecutatingResetSingleParameter();
        }


        private void TryExecutatingResetSingleParameter()
        {
            if (IsConnected)
            {
                MessageBox.Show("To reset a connected paramter, you first have to disconnect it.");
                return;
            }

            var removeAnimationCommand = new RemoveAnimationCommand(ValueHolder, 0);
            var setValueToDefaultCmd = new ResetInputToDefault(ValueHolder);
            Framefield.Core.ICommand[] commands = { removeAnimationCommand, setValueToDefaultCmd };

            App.Current.UndoRedoStack.AddAndExecute(new MacroCommand("Reset Paramter", commands));
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        private void UpdateGUI()
        {
            float newValue = 0;
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
            
            UpdateDropdownSelection(newValue);

            var color = Brushes.White;

            if (IsAnimated)
            {
                color = Brushes.Orange;
            }
            else if (ValueHolder.Connections.Count > 0)
            {
                color = Brushes.Green;
            }
            else if (ValueHolder.IsDefaultFuncSet)
            {
                color = Brushes.White.Clone();
                color.Opacity = 0.3;
            }
            XComboBox.Foreground = color;
        }


        private void UpdateDropdownSelection(float newValue)
        {
            var enumValues = _metaInput.EnumValues;
            var itemIndex = 0;

            _changeEventsEnabled = false;

            foreach (var enumEntry in enumValues)
            {
                if (newValue == (int) enumEntry.Value)
                {
                    XComboBox.SelectedIndex = itemIndex;
                    break;
                }
                ++itemIndex;
            }
            _changeEventsEnabled = true;
        }


        private void ValueHolder_ManipulatedHandler(object sender, EventArgs args)
        {
            UpdateGUI();
        }

        private void ValueHolder_ChangedHandler(object sender, EventArgs e)
        {
            UpdateGUI();
        }


        public bool IsAnimated
        {
            get { return _animationCurve != null; }
        }

        public  bool IsLocked()
        {
            return IsConnected;
        }

        private bool IsConnected
        {
            get { return _animationCurve == null && ValueHolder != null && ValueHolder.Connections.Count > 0; }
        }

        private bool _changeEventsEnabled = true;
        private MetaInput _metaInput;
        private ICurve _animationCurve = null;

    }
}

