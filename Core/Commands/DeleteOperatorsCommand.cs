// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Framefield.Core.Curve;
using Newtonsoft.Json;


namespace Framefield.Core.Commands
{

    [JsonObject(MemberSerialization.OptIn)]
    public class DeleteOperatorsCommand : ICommand
    {
        public string Name { get { return "Delete Operators"; } }
        public bool IsUndoable { get { return true; } }

        public DeleteOperatorsCommand() { }

        public DeleteOperatorsCommand(Operator parent, IEnumerable<Operator> opsToDelete)
        {
            _parentMetaID = parent.Definition.ID;
            foreach (var op in opsToDelete)
            {
                var animatedCurvesOfOperator = op.InternalParts.OfType<ICurve>().Where(curve => curve.GetPoints().Any());
                var keyframesToDelete = new List<Tuple<double, ICurve>>();
                foreach (var curve in animatedCurvesOfOperator)
                {
                    var keyframesOfCurve = (from point in curve.GetPoints()
                                            select new Tuple<double, ICurve>(point.Key, curve)).ToList();
                    keyframesToDelete.AddRange(keyframesOfCurve);
                }
                _removeKeyframesCommand = new RemoveKeyframeCommand(keyframesToDelete, 0);

                _deletedOpsMetaIDs.Add(op.Definition.ID);
                _deletedOpsInstanceIDs.Add(op.ID);
                var opStateEntries = new Dictionary<Guid, IOperatorPartState>();
                _deletedStates.Add(op.ID, opStateEntries);
                foreach (var state in parent.Definition.Operators[op.ID].Item2.OperatorPartStates)
                {
                    opStateEntries.Add(state.Key, (state.Value.Clone()));
                }

                var opParts = new Dictionary<Guid, IValue>();
                _deletedOpParts.Add(op.ID, opParts);
                foreach (var operatorPart in op.Inputs)
                {
                    opParts.Add(operatorPart.ID, (operatorPart.Func as Utilities.ValueFunction).Value);
                }

                _positions.Add(op.Position);
                _widths.Add((int) op.Width);
                _visibilities.Add(op.Visible);
                _deletedOpName = op.Name;
            }
            // find connections to/from the ops that are going to be deleted
            _connectionsToDeletedOps = (from op in _deletedOpsInstanceIDs
                                        from con in parent.Definition.Connections
                                        where con.SourceOpID == op || con.TargetOpID == op
                                        select con).Distinct().ToList();
        }


        public void Undo()
        {
            var parent = MetaManager.Instance.GetMetaOperator(_parentMetaID);

            // add ops
            for (var i = 0; i < _deletedOpsMetaIDs.Count; ++i)
            {
                var opToAdd = MetaManager.Instance.GetMetaOperator(_deletedOpsMetaIDs[i]);
                var pos = _positions[i];
                var width = _widths[i];
                var visible = _visibilities[i];
                parent.AddOperator(opToAdd, _deletedOpsInstanceIDs[i], pos.X, pos.Y, width, visible);

                var instanceProperties = parent.Operators[_deletedOpsInstanceIDs[i]].Item2;
                foreach (var opStateEntry in _deletedStates[_deletedOpsInstanceIDs[i]])
                    instanceProperties.OperatorPartStates[opStateEntry.Key] = opStateEntry.Value;

                var opToAddInstance = opToAdd.GetOperatorInstance(_deletedOpsInstanceIDs[i]);
                opToAddInstance.Name = _deletedOpName;
                foreach (var deletedOpPart in _deletedOpParts[_deletedOpsInstanceIDs[i]])
                {
                    var opPartToUpdate = (from input in opToAddInstance.Inputs where input.ID == deletedOpPart.Key select input).Single();
                    // this comparison could lead to problems with serialization
                    if ((opPartToUpdate.DefaultFunc as Utilities.ValueFunction).Value != deletedOpPart.Value)
                    {
                        var updateCommand = new UpdateOperatorPartValueFunctionCommand(opPartToUpdate, deletedOpPart.Value);
                        updateCommand.Do();
                    }
                }
            }

            // add connections;
            // todo: add multi input stuff, this here will only work for non multi inputs
            foreach (var con in _connectionsToDeletedOps)
            {
                parent.InsertConnectionAt(con);
            }

            _removeKeyframesCommand.Undo();
        }

        public void Do()
        {
            _removeKeyframesCommand.Do();
            
            var parent = MetaManager.Instance.GetMetaOperator(_parentMetaID);
            // now delete ops
            foreach (var op in _deletedOpsInstanceIDs)
            {
                parent.RemoveOperator(op);
            }
        }
        
        [JsonProperty]
        private Guid _parentMetaID;
        [JsonProperty]
        private List<Guid> _deletedOpsMetaIDs = new List<Guid>();
        [JsonProperty]
        private List<Point> _positions = new List<Point>();
        [JsonProperty]
        private List<int> _widths = new List<int>();
        [JsonProperty]
        private List<bool> _visibilities = new List<bool>();
        [JsonProperty]
        private List<MetaConnection> _connectionsToDeletedOps;
        [JsonProperty]
        private List<Guid> _deletedOpsInstanceIDs = new List<Guid>();
        [JsonProperty]
        private RemoveKeyframeCommand _removeKeyframesCommand;
        [JsonProperty(TypeNameHandling = TypeNameHandling.Auto)] // dict<op instance id, dict<input id, opstate>>
        private Dictionary<Guid, Dictionary<Guid, IOperatorPartState>> _deletedStates = new Dictionary<Guid, Dictionary<Guid, IOperatorPartState>>();
        [JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
        private Dictionary<Guid, Dictionary<Guid, IValue>> _deletedOpParts = new Dictionary<Guid, Dictionary<Guid, IValue>>();

        private string _deletedOpName;
    }

}
