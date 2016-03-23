// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Framefield.Core.Commands
{

    public class InsertOperatorCommand : MacroCommand
    {
        public InsertOperatorCommand() { }

        public InsertOperatorCommand(OperatorPart sourceOutput, OperatorPart targetInput, Operator compositionOp, Operator opToInsert, int multiInputIndexAtTarget)
        {
            _name = "Insert Operator";

            var usedSingleInputs = new List<MetaInput>();
            var usedMultiInputsCounter = new Dictionary<MetaInput, int>();
            var compositionMeta = opToInsert.Parent.Definition;

            if (opToInsert.Outputs.Count == 1)
            {
                var sourceOp = sourceOutput.Parent;
                var targetOp = targetInput.Parent;
                var sourceOpID = (sourceOp.ID == compositionOp.ID) ? Guid.Empty : sourceOp.ID;
                var targetOpID = (targetOp.ID == compositionOp.ID) ? Guid.Empty : targetOp.ID;

                var sourceOpPartID = sourceOutput.ID;
                var targetOpPartID = targetInput.ID;

                var inputOpType = sourceOutput.Type;
                Func<FunctionType, bool> isValidInputType = type => inputOpType == type || type == FunctionType.Generic;
                Func<MetaInput, bool> isInputUsable = input => input.IsMultiInput || !usedSingleInputs.Contains(input);
                var matchingTargetInput = (from input in opToInsert.Definition.Inputs
                                           where isValidInputType(input.OpPart.Type) && isInputUsable(input)
                                           select input).FirstOrDefault();

                if (matchingTargetInput == null)
                    return;

                // extract previous connection
                var matchingConnections = (from con in compositionMeta.Connections
                                           where con.TargetOpPartID == targetOpPartID && con.TargetOpID == targetOpID
                                           select con).ToList();

                var prevConnection = matchingConnections[multiInputIndexAtTarget];

                // Split existing connections for single selected operators
                if (prevConnection != null)
                {
                    var conNewOpToPrevTarget = new MetaConnection(opToInsert.ID, opToInsert.Definition.Outputs[0].ID, prevConnection.TargetOpID, prevConnection.TargetOpPartID);
                    _commands.Add(new RemoveConnectionCommand(compositionMeta, prevConnection, multiInputIndexAtTarget));
                    _commands.Add(new InsertConnectionCommand(compositionMeta, conNewOpToPrevTarget, multiInputIndexAtTarget));
                }

                // insert new connection
                var newConnection = new MetaConnection(sourceOpID, sourceOpPartID, opToInsert.ID, matchingTargetInput.ID);
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
                _commands.Add(new InsertConnectionCommand(compositionMeta, newConnection, index));
            }
        }

    }
}
