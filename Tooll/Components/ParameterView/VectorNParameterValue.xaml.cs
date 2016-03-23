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
using SharpDX;
using ICommand = Framefield.Core.ICommand;


namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for VectorNParameterValue.xaml
    /// </summary>
    public partial class VectorNParameterValue : UserControl
    {
        public VectorNParameterValue(OperatorPart[] opParts) {
            InitializeComponent();
            _operatorParts = opParts;
            _keepValuesBeforeManipulation = new List<float>();
        }


        private ICommand BuildManipulationCommand(FloatParameterControl control, float newValue)
        {
            ICommand cmd;
            if (control.IsAnimated)
            {
                cmd = new AddOrUpdateKeyframeCommand(App.Current.Model.GlobalTime, newValue, control.ValueHolder);
            }
            else
            {
                cmd = new UpdateOperatorPartValueFunctionCommand(control.ValueHolder, new Float(newValue));
            }
            _commandsForControls[control] = cmd;
            return cmd;
        }


        private void UpdateManipulationCommand(FloatParameterControl control, float newValue)
        {
            var cmd = _commandsForControls[control];
            if (cmd is AddOrUpdateKeyframeCommand)
            {
                var addKeyframeCommand = cmd as AddOrUpdateKeyframeCommand;
                addKeyframeCommand.KeyframeValue.Value = newValue;
            }
            else
            {
                var updateValueCommand = cmd as UpdateOperatorPartValueFunctionCommand;
                updateValueCommand.Value = new Float(newValue);
            }
        }


        public void StartManipulationHandler(object sender, EventArgs e)
        {
            _keepValuesBeforeManipulation.Clear();
            _commandsForControls = new Dictionary<FloatParameterControl, ICommand>();

            App.Current.MainWindow.CompositionView.XTimeView.XAnimationCurveEditor.DisableCurveUpdatesOnModifiedEvent = true;

            var commandList = new List<ICommand>();
            foreach (var control in _parameterControls)
            {
                var v = control.Value;
                commandList.Add(BuildManipulationCommand(control, v));
                _keepValuesBeforeManipulation.Add(v);

            }

            _updateValueGroupMacroCommand = new MacroCommand("Update parameters group", commandList);
            _updateValueGroupMacroCommand.Do();
        }


        public void UpdateManipulationHandler(object sender, ParameterGroupManipulatedEventArgs e)
        {
            var factor = (float)(e.Offset * UIHelper.SubScaleFromKeyboardModifiers() * 0.01);
            int index = 0;
            foreach (var opPart in _operatorParts)
            {
                var metaInput = opPart.Parent.GetMetaInput(opPart);
                var newValue = _keepValuesBeforeManipulation[index] * (factor * metaInput.Scale+1);
                newValue = Core.Utilities.Clamp(newValue, metaInput.Min, metaInput.Max);
                UpdateManipulationCommand(_parameterControls[index], newValue);
                index++;
            }
            App.Current.UpdateRequiredAfterUserInteraction = true;
            
            _updateValueGroupMacroCommand.Do();  

        }

        public void EndManipulationHandler(object sender, EventArgs e)
        {
            App.Current.MainWindow.CompositionView.XTimeView.XAnimationCurveEditor.DisableCurveUpdatesOnModifiedEvent = false;

            App.Current.UndoRedoStack.Add(_updateValueGroupMacroCommand);
            _updateValueGroupMacroCommand = null;
        }

        /**
         * Notes notes on group manipulation:
         * - This is triggered by the XParameterName Button in OperatorParameterViewRow. This emits events for reset and value manipulation.
         * This binding between these events and the following handlers is done in Constructor of the OperatorParameterView
         */
        public void ResetToDefaultHandler(object sender, EventArgs e)
        {
            var entries = new List<OperatorPart>();
            foreach (var op in _operatorParts)
            {
                entries.Add(op);
            }
            var command = new ResetInputToGroupCommand(entries);
            App.Current.UndoRedoStack.AddAndExecute(command);
            App.Current.UpdateRequiredAfterUserInteraction = true;
        }




        private void OnLoaded(object sender, RoutedEventArgs e) {
            int i = 0;
            XGrid.ColumnDefinitions.Clear();
            foreach (var opPart in _operatorParts) {
                XGrid.ColumnDefinitions.Add(new ColumnDefinition() {MinWidth = 25});
                var newControl = new FloatParameterControl(opPart);
                Grid.SetColumn(newControl, i++);
                _parameterControls.Add(newControl);
                newControl.TabMoveEvent += TabMoveEventHandler;
                XGrid.Children.Add(newControl);
            }
        }

        void TabMoveEventHandler(FloatParameterControl sender, bool backwards)
        {
            int index = _parameterControls.IndexOf(sender);
            if (index != -1)
            {
                index += backwards ? -1 : 1;
                index += _parameterControls.Count;
                index %= _parameterControls.Count();
                _parameterControls[index].SwitchToTextEdit();
            }
        }

        private List<float> _keepValuesBeforeManipulation;
        private MacroCommand _updateValueGroupMacroCommand;
        private Dictionary<FloatParameterControl, ICommand> _commandsForControls;
        private OperatorPart[] _operatorParts;
        private readonly List<FloatParameterControl> _parameterControls = new List<FloatParameterControl>();
    }
}
