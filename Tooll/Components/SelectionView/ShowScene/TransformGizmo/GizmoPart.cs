// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.Collections.Generic;
using Framefield.Core;
using Framefield.Core.Commands;
using Framefield.Core.Curve;
using SharpDX;

namespace Framefield.Tooll.Components.SelectionView.ShowScene.TransformGizmo
{
    internal abstract class GizmoPart
    {
        public int Index { get; set; }
        public Dictionary<GizmoParameterIds, GizmoParameter> RelavantGizmoParameters { get; protected set; }

        public abstract bool HitTestWithRayInObject(Ray rayInObject, out float hitDistance);

        public abstract void DragUpdate(Ray rayInObject, Matrix gizmoToParent);

        public void StartManimpulation()
        {
            _commandsForInputs = new Dictionary<OperatorPart, ICommand>();
            _parameterValuesBeforeDrag = new Dictionary<GizmoParameterIds, float>();

            var commandList = new List<ICommand>();

            foreach (var pair in RelavantGizmoParameters)
            {
                GizmoParameter parameter = pair.Value;
                _parameterValuesBeforeDrag[pair.Key] = parameter.Value;
                commandList.Add(BuildManipulationCommand(parameter.Input, parameter.Value));
            }

            _updateValueGroupMacroCommand = new MacroCommand("Update Transform Gizmo", commandList);
            _updateValueGroupMacroCommand.Do();
        }

        public void UpdateManipulation(Ray rayInObject, Matrix gizmoToParent)
        {
            DragUpdate(rayInObject, gizmoToParent);

            foreach (var pair in RelavantGizmoParameters)
            {
                GizmoParameter parameter = pair.Value;
                UpdateManipulationCommand(parameter.Input, parameter.Value);
            }
            _updateValueGroupMacroCommand.Do();
        }

        public void CompleteManipulation()
        {
            if (_updateValueGroupMacroCommand != null)
                App.Current.UndoRedoStack.Add(_updateValueGroupMacroCommand);
            else
                Logger.Warn("Completing GizmoPartManipulation without initialized command?");

            _updateValueGroupMacroCommand = null;
        }


        private ICommand BuildManipulationCommand(OperatorPart input, float newValue)
        {
            ICommand cmd;

            OperatorPart animationOpPart = Animation.GetRegardingAnimationOpPart(input);
            if (animationOpPart != null && animationOpPart.Func is ICurve)
            {
                cmd = new AddOrUpdateKeyframeCommand(App.Current.Model.GlobalTime, newValue, input);
            }
            else
            {
                cmd = new UpdateOperatorPartValueFunctionCommand(input, new Float(newValue));
            }
            _commandsForInputs[input] = cmd;
            return cmd;
        }


        private void UpdateManipulationCommand(OperatorPart input, float newValue)
        {
            ICommand cmd = _commandsForInputs[input];
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

        protected Dictionary<GizmoParameterIds, float> _parameterValuesBeforeDrag;

        private Dictionary<OperatorPart, ICommand> _commandsForInputs;
        private MacroCommand _updateValueGroupMacroCommand;
    }
}