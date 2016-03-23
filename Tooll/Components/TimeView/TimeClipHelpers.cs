// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Framefield.Core;
using Framefield.Core.Commands;
using Framefield.Core.OperatorPartTraits;

namespace Framefield.Tooll.Components.TimeView
{
    public static class TimeClipHelpers
    {

        const int HACK_TIMECLIP_STARTTIME_PARAM_INDEX = 1;
        const int HACK_TIMECLIP_ENDTIME_PARAM_INDEX = 2;
        const int HACK_TIMECLIP_SOURCEIN_PARAM_INDEX = 3;
        const int HACK_TIMECLIP_SOURCEOUT_PARAM_INDEX = 4;
        const int HACK_TIMECLIP_SOURCEOUT_LAYER_ID = 5;
        const float MIN_SEGMENT_DURATION = 1.0f / 60.0f;

        /**
         * This is an extremely rough stub for testing the TimeClip splitting functionality.
         * Need fixes for...
         * - duplicate connections of original op
         * - get rid of magic numbers of parameters
         * - fix potential problems when parameters are animated of connected
         * - move this to an Memento-Operation
         * - resolve direct dependencies to CompositionGraphView an it's members
         */
        public static void SplitSelectedTimeClips()
        {
            var nextSelection = new List<ISelectable>();
            var currentTime = (float)App.Current.Model.GlobalTime;

            var selectedOpWidgets = new List<OperatorWidget>();
            foreach (var element in App.Current.MainWindow.CompositionView.CompositionGraphView.SelectionHandler.SelectedElements)
            {
                var opWidget = element as OperatorWidget;

                if (opWidget == null || !(opWidget.Operator.InternalParts[0].Func is ITimeClip))
                    continue;

                selectedOpWidgets.Add(opWidget);
            }

            var commandList = new List<ICommand>();
            foreach (var opWidget in selectedOpWidgets)
            {
                var op = opWidget.Operator;
                if (op == null)
                    continue;

                var startTime = op.Inputs[HACK_TIMECLIP_STARTTIME_PARAM_INDEX].Eval(new OperatorPartContext()).Value;
                var endTime = op.Inputs[HACK_TIMECLIP_ENDTIME_PARAM_INDEX].Eval(new OperatorPartContext()).Value;
                var sourceIn = op.Inputs[HACK_TIMECLIP_SOURCEIN_PARAM_INDEX].Eval(new OperatorPartContext()).Value;
                var sourceOut = op.Inputs[HACK_TIMECLIP_SOURCEOUT_PARAM_INDEX].Eval(new OperatorPartContext()).Value;
                var layerIndex = op.Inputs[HACK_TIMECLIP_SOURCEOUT_LAYER_ID].Eval(new OperatorPartContext()).Value;
                var sourceCutTime = (currentTime - startTime) / (endTime - startTime) * (sourceOut - sourceIn) + sourceIn;

                if (!(startTime + MIN_SEGMENT_DURATION < currentTime) || !(currentTime < endTime - MIN_SEGMENT_DURATION))
                    continue;

                nextSelection.Add(opWidget);

                // Cut current op
                var currentOpSetEndtimeCommand = new UpdateOperatorPartValueFunctionCommand(op.Inputs[HACK_TIMECLIP_ENDTIME_PARAM_INDEX], new Core.Float(currentTime));
                currentOpSetEndtimeCommand.Do();

                var currentOpSetSourceOutCommand = new UpdateOperatorPartValueFunctionCommand(op.Inputs[HACK_TIMECLIP_SOURCEOUT_PARAM_INDEX], new Core.Float(sourceCutTime));
                currentOpSetSourceOutCommand.Do();

                // Intitial new op
                var compositionOp = App.Current.MainWindow.CompositionView.CompositionGraphView.CompositionOperator;

                var addNewOpCommand = new AddOperatorCommand(compositionOp, op.Definition.ID, op.Position.X + 10, op.Position.Y + 10);
                addNewOpCommand.Do();

                var newOpWidget = App.Current.MainWindow.CompositionView.CompositionGraphView.FindOperatorWidgetById(addNewOpCommand.AddedInstanceID);

                nextSelection.Add(newOpWidget);
                var newOp = newOpWidget.Operator;

                if (!String.IsNullOrEmpty(op.Name))
                    newOp.Name = Utilities.GetDuplicatedTitle(op.Name);

                var newOpSetStartTimeCommand = new UpdateOperatorPartValueFunctionCommand(newOp.Inputs[HACK_TIMECLIP_STARTTIME_PARAM_INDEX], new Core.Float(currentTime));
                newOpSetStartTimeCommand.Do();

                var newOpSetEndTimeCommand = new UpdateOperatorPartValueFunctionCommand(newOp.Inputs[HACK_TIMECLIP_ENDTIME_PARAM_INDEX], new Core.Float(endTime));
                newOpSetEndTimeCommand.Do();

                var newOpSetSourceInCommand = new UpdateOperatorPartValueFunctionCommand(newOp.Inputs[HACK_TIMECLIP_SOURCEIN_PARAM_INDEX], new Core.Float(sourceCutTime));
                newOpSetSourceInCommand.Do();

                var newOpSetSourceOutCommand = new UpdateOperatorPartValueFunctionCommand(newOp.Inputs[HACK_TIMECLIP_SOURCEOUT_PARAM_INDEX], new Core.Float(sourceOut));
                newOpSetSourceOutCommand.Do();

                var newOpSetLayerCommand = new UpdateOperatorPartValueFunctionCommand(newOp.Inputs[HACK_TIMECLIP_SOURCEOUT_LAYER_ID], new Float(layerIndex));
                newOpSetLayerCommand.Do();

                commandList.AddRange(new ICommand[]
                {   currentOpSetEndtimeCommand,
                    currentOpSetSourceOutCommand,
                    addNewOpCommand, 
                    newOpSetStartTimeCommand, 
                    newOpSetEndTimeCommand, 
                    newOpSetSourceInCommand, 
                    newOpSetSourceOutCommand,
                    newOpSetLayerCommand
                });
            }
            App.Current.UndoRedoStack.Add(new MacroCommand("Split time clip", commandList));

            if (nextSelection.Any())
                App.Current.MainWindow.CompositionView.CompositionGraphView.SelectionHandler.SetElements(nextSelection);
        }

    }
}
