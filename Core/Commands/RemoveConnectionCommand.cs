// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Framefield.Core.Commands
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RemoveConnectionCommand : ICommand
    {
        public string Name { get { return "Remove Connection"; } }
        public bool IsUndoable { get { return true; } }

        public RemoveConnectionCommand() { }

        public RemoveConnectionCommand(Operator op, Connection connectionToRemove)
        {
            _opMetaID = op.Definition.ID;
            _metaConnectionToRemove = new MetaConnection(connectionToRemove);
            if (_metaConnectionToRemove.SourceOpID == op.ID)
                _metaConnectionToRemove.SourceOpID = Guid.Empty;
            if (_metaConnectionToRemove.TargetOpID == op.ID)
                _metaConnectionToRemove.TargetOpID = Guid.Empty;
            _connectionIndex = connectionToRemove.Index;
        }

        public RemoveConnectionCommand(MetaOperator metaOp, MetaConnection connectionToRemove, int index)
        {
            _opMetaID = metaOp.ID;
            _metaConnectionToRemove = connectionToRemove;
            _connectionIndex = index;
        }

        public void Undo()
        {
            var op = MetaManager.Instance.GetMetaOperator(_opMetaID);
            op.InsertConnectionAt(_metaConnectionToRemove, _connectionIndex);
        }

        public void Do()
        {
            var op = MetaManager.Instance.GetMetaOperator(_opMetaID);
            op.RemoveConnection(_metaConnectionToRemove, _connectionIndex);
        }

        [JsonProperty]
        private Guid _opMetaID;
        [JsonProperty]
        private MetaConnection _metaConnectionToRemove;
        [JsonProperty]
        private int _connectionIndex;
    }

}
