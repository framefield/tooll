// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Newtonsoft.Json;


namespace Framefield.Core.Commands
{
    [JsonObject(MemberSerialization.OptIn)]
    public class AddOperatorCommand : ICommand
    {
        public string Name { get { return "Add Operator"; } }
        public bool IsUndoable { get { return true; } }
        public Guid AddedInstanceID { get { return _addedOpInstanceID; } }
        public Point Position { get; set; }
        public double Width { get; set; }
        public string InstanceName { get; set; }
        public Dictionary<Guid, IOperatorPartState> OperatorPartStates { get { return _operatorPartStates; } set { _operatorPartStates = value;  } }

        public AddOperatorCommand() { }

        public AddOperatorCommand(Operator parent, Guid opIDToAdd, double posX = 100, double posY = 100, double width = 100, bool visible = true, string instanceName = "")
        {
            _parentMetaID = parent.Definition.ID;
            _opToAddMetaID = opIDToAdd;
            _addedOpInstanceID = Guid.NewGuid();
            Position = new Point(posX, posY);
            Width = width;
            InstanceName = instanceName;
            _visible = visible;
        }

        public AddOperatorCommand(MetaOperator compositionOp, Guid opIDToAdd, double posX = 100, double posY = 100, double width = 100, bool visible = true, string instanceName = "")
        {
            _parentMetaID = compositionOp.ID;
            _opToAddMetaID = opIDToAdd;
            _addedOpInstanceID = Guid.NewGuid();
            Position = new Point(posX, posY);
            Width = width;
            InstanceName = instanceName;
            _visible = visible;
        }

        public void Undo()
        {
            var parent = MetaManager.Instance.GetMetaOperator(_parentMetaID);
            parent.RemoveOperator(_addedOpInstanceID);
        }

        public void Do()
        {
            var parent = MetaManager.Instance.GetMetaOperator(_parentMetaID);
            var opToAdd = MetaManager.Instance.GetMetaOperator(_opToAddMetaID);
            parent.AddOperator(opToAdd, _addedOpInstanceID, Position.X, Position.Y, Width, _visible);

            foreach (var opStateEntry in _operatorPartStates)
                parent.Operators[_addedOpInstanceID].Item2.OperatorPartStates[opStateEntry.Key] = opStateEntry.Value;

            parent.Operators[_addedOpInstanceID].Item2.Name = InstanceName;
        }

        [JsonProperty]
        private Guid _parentMetaID;
        [JsonProperty]
        private Guid _opToAddMetaID;
        [JsonProperty]
        private bool _visible;
        [JsonProperty]
        private Guid _addedOpInstanceID;
        [JsonProperty(TypeNameHandling = TypeNameHandling.Auto)] // dict<input id, opstate>
        private Dictionary<Guid, IOperatorPartState> _operatorPartStates = new Dictionary<Guid, IOperatorPartState>();
    }

}
