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
    public class AddTimeMarkerCommand : ICommand
    {
        public string Name { get { return "Add TimeMarker"; } }
        public bool IsUndoable { get { return true; } }

        public AddTimeMarkerCommand() { }

        public AddTimeMarkerCommand(Operator parent, Guid opIDToAdd, double time, int posX = 100, int posY = 100, int width = 100, bool visible = true) {
            _parentMetaID = parent.Definition.ID;
            _opToAddMetaID = opIDToAdd;
            _addedOpInstanceID = Guid.NewGuid();
            _pos = new Point(posX, posY);
            _width = width;
            _visible = visible;

            // diff from AddOperatorCommand
            _time = time;
        }

        public void Undo() {
            var parent = MetaManager.Instance.GetMetaOperator(_parentMetaID);
            parent.RemoveOperator(_addedOpInstanceID);
        }

        public void Do() {
            var parent = MetaManager.Instance.GetMetaOperator(_parentMetaID);
            var opToAdd = MetaManager.Instance.GetMetaOperator(_opToAddMetaID);
            parent.AddOperator(opToAdd, _addedOpInstanceID, _pos.X, _pos.Y, _width, _visible);

            // diff from AddOperatorCommand
            var addedOp = opToAdd.GetOperatorInstance(_addedOpInstanceID);
            var timeMarker = addedOp.InternalParts[0].Func as OperatorPartTraits.ITimeMarker;
            if (timeMarker != null)
                timeMarker.Time = _time;
        }

        [JsonProperty]
        private Guid _parentMetaID;
        [JsonProperty]
        private Guid _opToAddMetaID;
        [JsonProperty]
        private Point _pos;
        [JsonProperty]
        private int _width;
        [JsonProperty]
        private bool _visible;
        [JsonProperty]
        private Guid _addedOpInstanceID;

        // diff from AddOperatorCommand
        [JsonProperty]
        private double _time;
    }

}
