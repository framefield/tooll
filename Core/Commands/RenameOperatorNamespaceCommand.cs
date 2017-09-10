// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Framefield.Core.Commands
{
    public class RenameOperatorNamespaceCommand : ICommand
    {
        public string Name => "Rename Namespace";
        public bool IsUndoable => true;

        public RenameOperatorNamespaceCommand() { }

        public RenameOperatorNamespaceCommand(MetaOperator metaOp, string newNamespace)
            : this(new[] { metaOp }, new[] { newNamespace })
        {
        }

        public RenameOperatorNamespaceCommand(IEnumerable<MetaOperator> metaOps, IEnumerable<string> newNamespaces)
        {
            foreach (var op in metaOps)
            {
                _metaOperatorIDs.Add(op.ID);
                _previousNamespaces.Add(op.Namespace);
            }
            _newNamespaces = newNamespaces.ToList();
        }

        public void Do()
        {
            for (int idx = 0; idx < _metaOperatorIDs.Count; ++idx)
            {
                var metaOp = MetaManager.Instance.GetMetaOperator(_metaOperatorIDs[idx]);
                metaOp.Namespace = _newNamespaces[idx];
            }
        }

        public void Undo()
        {
            for (int idx = 0; idx < _metaOperatorIDs.Count; ++idx)
            {
                var metaOp = MetaManager.Instance.GetMetaOperator(_metaOperatorIDs[idx]);
                metaOp.Namespace = _previousNamespaces[idx];
            }
        }

        [JsonProperty]
        private List<Guid> _metaOperatorIDs = new List<Guid>();
        [JsonProperty]
        private List<string> _previousNamespaces = new List<string>();
        [JsonProperty]
        private List<string> _newNamespaces = new List<string>();
    }
}
