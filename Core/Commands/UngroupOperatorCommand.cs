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
    public class UngroupOperatorCommand : ICommand
    {
        public string Name { get { return "Ungroup Operator"; } }
        public bool IsUndoable { get { return false; } }

        public UngroupOperatorCommand() { }

        public UngroupOperatorCommand(Operator opToUngroup)
        {
            _compositionOpMetaID = opToUngroup.Parent.Definition.ID;
            _opToUngroupMetaID = opToUngroup.Definition.ID;
            _opToUngroupInstanceID = opToUngroup.ID;
            _opToUngroupName = opToUngroup.Name == string.Empty ? opToUngroup.Definition.Name : opToUngroup.Name;
            _opToUngroupPosition = opToUngroup.Position;
            _ungroupedOpsInstanceIDs = (from op in opToUngroup.InternalOps select op.ID).ToList();
        }

        public void Undo()
        {
//            var compositionMetaOp = MetaManager.Instance.GetMetaOperator(_compositionOpMetaID);
//            var combinedMetaOp = compositionMetaOp.CombineToNewOp(_opToUngroupMetaID, _opToUngroupInstanceID,  _ungroupedOpsInstanceIDs, _opToUngroupName, _opToUngroupPosition.X, _opToUngroupPosition.Y);
        }

        public void Do()
        {
            var compositionMetaOp = MetaManager.Instance.GetMetaOperator(_compositionOpMetaID);
            var oldToNewIds = compositionMetaOp.UngroupOperator(_opToUngroupInstanceID, MetaManager.Instance.GetMetaOperator(Guid.Parse("7424a910-d647-4049-9fda-78189bcaa964")));
            _ungroupedOpsInstanceIDs.Clear();
            foreach (var idEntry in oldToNewIds)
            {
                _ungroupedOpsInstanceIDs.Add(idEntry.Value);
            }
        }

        [JsonProperty]
        string _opToUngroupName = string.Empty;
        [JsonProperty]
        Point _opToUngroupPosition;
        [JsonProperty]
        Guid _compositionOpMetaID;
        [JsonProperty]
        Guid _opToUngroupMetaID;
        [JsonProperty]
        Guid _opToUngroupInstanceID;
        [JsonProperty]
        List<Guid> _ungroupedOpsInstanceIDs;
    }
}
