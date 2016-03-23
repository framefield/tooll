// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Framefield.Core.Commands
{
    /**
     * Automatic connection focuses on the obvious applications and ignores
     * fringe cases. We do...
     * 
     * 1. No Op was selected
     * -> Find free spot in center of screen
     * 
     * 2. One Op was selected
     * -> iterate over all required or relevant inputs of new op
     *   -> connect if type matches and singleField
     *   -> connect if type matches and multifield
     * -> Stack op, if connected or find free spot, if not
     * 
     * 3. Op(s) were selected
     * -> iterate over all selected operators
     *   -> take first output of op
     *   -> iterate over all required or relevant inputs of new op
     *      -> if type matches and, single field and input is free
     *          -> if already ou
     *      -> connect if type matches, single field and free
     *      -> append if type matches, and multifield 
     * -> place op on a free spot above connected operators
     */
    public class AddOperatorAndConnectToInputsCommand : MacroCommand
    {
        public AddOperatorAndConnectToInputsCommand() { }

        public AddOperatorAndConnectToInputsCommand(Operator compositionOp, MetaOperator metaOpToAdd, IEnumerable<Operator> inputOps, Point position)
        {
            _name = "Add Operator And Connect";

            var addOperatorCommand = new AddOperatorCommand(compositionOp, metaOpToAdd.ID);
            _commands.Add(addOperatorCommand);

            var addedInstanceID = addOperatorCommand.AddedInstanceID;
            double maxY = Double.NegativeInfinity;
            double sumX = 0;

            var connectedOps = new List<Operator>();
            var usedSingleInputs = new List<MetaInput>();
            var usedMultiInputsCounter = new Dictionary<MetaInput, int>();

            // select usable input ops, the ones with at least one output - for now
            var usableInputOps = from op in inputOps where op.Outputs.Count > 0 select op;

            foreach (var inputOp in usableInputOps)
            {
                maxY = Math.Max(maxY, inputOp.Position.Y);
                sumX += inputOp.Position.X;

                var sourceOpID = inputOp.ID;
                var sourceOpPartID = inputOp.Outputs[0].ID;

                var inputOpType = inputOp.Definition.Outputs[0].OpPart.Type;
                Func<FunctionType, bool> isValidInputType = type => { return inputOpType == type || type == FunctionType.Generic; };
                Func<MetaInput, bool> isInputUsable = input => { return input.IsMultiInput || !usedSingleInputs.Contains(input); };
                var matchingTargetInput = (from input in metaOpToAdd.Inputs
                                           where isValidInputType(input.OpPart.Type) && isInputUsable(input)
                                           select input).FirstOrDefault();

                if (matchingTargetInput == null)
                    continue; // try next input op

                var prevConnection = (from con in compositionOp.Definition.Connections
                                      where con.SourceOpPartID == sourceOpPartID && con.SourceOpID == sourceOpID
                                      select con).FirstOrDefault();

                // Split existing connections for single selected operators
                if (prevConnection != null && inputOps.Count() == 1)
                {
                    var firstOccuranceOfTargetOpID = compositionOp.Definition.Connections.FindIndex(con => (con.TargetOpID == prevConnection.TargetOpID) &&
                                                                                                           (con.TargetOpPartID == prevConnection.TargetOpPartID));
                    var idxOfPrevConnection = compositionOp.Definition.Connections.FindIndex(con => (con.SourceOpID == prevConnection.SourceOpID) &&
                                                                                                    (con.SourceOpPartID == prevConnection.SourceOpPartID) &&
                                                                                                    (con.TargetOpID == prevConnection.TargetOpID) &&
                                                                                                    (con.TargetOpPartID == prevConnection.TargetOpPartID));
                    int multiInputIdx = idxOfPrevConnection - firstOccuranceOfTargetOpID;

                    var conNewOpToPrevTarget = new MetaConnection(addedInstanceID, metaOpToAdd.Outputs[0].ID, prevConnection.TargetOpID, prevConnection.TargetOpPartID);
                    _commands.Add(new RemoveConnectionCommand(compositionOp.Definition, prevConnection, multiInputIdx));
                    _commands.Add(new InsertConnectionCommand(compositionOp.Definition, conNewOpToPrevTarget, multiInputIdx));
                }

                // insert new connection
                var newConnection = new MetaConnection(sourceOpID, sourceOpPartID, addedInstanceID, matchingTargetInput.ID);
                int index = 0;
                if (matchingTargetInput.IsMultiInput)
                {
                    usedMultiInputsCounter.TryGetValue(matchingTargetInput, out index);
                    usedMultiInputsCounter[matchingTargetInput] = index + 1;
                }
                else
                {
                    usedSingleInputs.Add(matchingTargetInput);
                }

                _commands.Add(new InsertConnectionCommand(compositionOp.Definition, newConnection, index));

                connectedOps.Add(inputOp);
            }

            var posX = position.X;
            var posY = position.Y;
            var width = 100.0;
            // Position above connected inputs operators
            if (connectedOps.Count == 0)
            {
                // find free position in center
            }
            else if (connectedOps.Count == 1)
            {
                posX = connectedOps[0].Position.X;
                posY = connectedOps[0].Position.Y - 25;
                addOperatorCommand.Width = width;
            }
            else if (connectedOps.Count > 1)
            {
                posX = sumX / inputOps.Count();
                posY = maxY - 40;
            }
            addOperatorCommand.Position = new Point(posX, posY);
        }
    }
}
