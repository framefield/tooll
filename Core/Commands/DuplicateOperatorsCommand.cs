// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Framefield.Core.Commands
{
    // This op is currently not needed anymore, but contains some interesting code how 'combine to new op' could be 
    // generated as macro command, therefore it's left in the core code base but made private instead!
    class DuplicateOperatorsCommand : MacroCommand
    {
        public DuplicateOperatorsCommand() { }

        public DuplicateOperatorsCommand(MetaOperator sourceCompositionOp, MetaOperator targetCompositionOp, IEnumerable<Operator> opsToDuplicate)
        {
            _name = "Duplicate Operators";

            _commands = new List<ICommand>();

            var offset = new Point(100, 100);

            var originalToCopyMap = new Dictionary<Guid, Guid>();
            var toDuplicate = opsToDuplicate as Operator[] ?? opsToDuplicate.ToArray();
            foreach (var op in toDuplicate)
            {
                var position = new Point(op.Position.X + offset.X, op.Position.Y + offset.Y);

                // copy op
                var addOperatorCmd = new AddOperatorCommand(targetCompositionOp, op.Definition.ID, position.X, position.Y, op.Width, op.Visible);
                foreach (var state in sourceCompositionOp.Operators[op.ID].Item2.OperatorPartStates)
                {
                    addOperatorCmd.OperatorPartStates.Add(state.Key, state.Value.Clone());
                }
                originalToCopyMap[op.ID] = addOperatorCmd.AddedInstanceID;
                _commands.Add(addOperatorCmd);

                // copy op properties
                var updatePropertiesCmd = new UpdateOperatorPropertiesCommand(op.Definition.ID, addOperatorCmd.AddedInstanceID,
                                                                              new UpdateOperatorPropertiesCommand.Entry(op) 
                                                                                  {
                                                                                      Position = position
                                                                                  });
                _commands.Add(updatePropertiesCmd);

                // copy input parameters
                foreach (var input in op.Inputs)
                {
                    if (!input.IsDefaultFuncSet)
                    {
                        // if default func is not set update the input value
                        var value = (input.Func as Utilities.ValueFunction).Value;
//                        var setInputValueCmd = new UpdateOperatorPartValueFunctionCommand(op.Definition.ID, addOperatorCmd.AddedInstanceID, input.ID,
//                                                                                          true, value);
//                        _commands.Add(setInputValueCmd);
                    }
                }
            }


            // copy connections between duplicated ops
            // first get the connections between the original ops
            var internalConnections = from con in sourceCompositionOp.Connections
                                      from sourceOp in toDuplicate
                                      from targetOp in toDuplicate
                                      where con.SourceOpID == sourceOp.ID
                                      where con.TargetOpID == targetOp.ID
                                      select con;

            // sort them as stored in parent op in order to preserve multi input order
            var allConnectionsSorted = new List<MetaConnection>(); // these are later on needed to restore multi input order!
            foreach (var con in sourceCompositionOp.Connections)
            {
                if (internalConnections.Contains(con))
                    allConnectionsSorted.Add(con);
            }

            // create a group for each input
            var groupedConnections = (from con in allConnectionsSorted
                                      group con by (con.TargetOpID.ToString() + con.TargetOpPartID.ToString())
                                      into g
                                      select g).ToList();

            // insert the connections to new op
            foreach (var conGroup in groupedConnections)
            {
                var index = 0;
                foreach (var con in conGroup)
                {
                    var conBetweenNewOps = new MetaConnection(originalToCopyMap[con.SourceOpID], con.SourceOpPartID,
                                                              originalToCopyMap[con.TargetOpID], con.TargetOpPartID);
                    _commands.Add(new InsertConnectionCommand(targetCompositionOp, conBetweenNewOps, index));
                    index++;
                }
            }

        }
    }
}
