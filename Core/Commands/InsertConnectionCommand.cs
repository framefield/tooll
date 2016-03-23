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
    public class InsertConnectionCommand : ICommand
    {
        public string Name { get { return "Insert Connection"; } }
        public bool IsUndoable { get { return true; } }

        public InsertConnectionCommand() { }

        public InsertConnectionCommand(Operator compositionOp, Connection connectionToInsert)
        {
            _opMetaID = compositionOp.Definition.ID;
            _metaConnectionToInsert = new MetaConnection(connectionToInsert);
            if (_metaConnectionToInsert.SourceOpID == compositionOp.ID)
                _metaConnectionToInsert.SourceOpID = Guid.Empty;
            if (_metaConnectionToInsert.TargetOpID == compositionOp.ID)
                _metaConnectionToInsert.TargetOpID = Guid.Empty;
            _connectionIndex = connectionToInsert.Index;
        }

        public InsertConnectionCommand(MetaOperator compositionMetaOp, MetaConnection connectionToInsert, int index)
        {
            _opMetaID = compositionMetaOp.ID;
            _metaConnectionToInsert = connectionToInsert;
            _connectionIndex = index;
        }

        public void Undo()
        {
            var op = MetaManager.Instance.GetMetaOperator(_opMetaID);
            op.RemoveConnection(_metaConnectionToInsert, _connectionIndex);
        }

        public void Do()
        {
            var op = MetaManager.Instance.GetMetaOperator(_opMetaID);
            op.InsertConnectionAt(_metaConnectionToInsert, _connectionIndex);
        }

        [JsonProperty]
        private Guid _opMetaID;
        [JsonProperty]
        private MetaConnection _metaConnectionToInsert;
        [JsonProperty]
        private int _connectionIndex;
    }

}
