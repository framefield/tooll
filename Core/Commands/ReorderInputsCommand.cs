// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Framefield.Core.Commands
{
    public class ReorderInputsCommand : ICommand
    {
        public string Name => "Reorder Inputs";
        public bool IsUndoable => true;

        public ReorderInputsCommand(Operator compOp, List<Guid> operatorPartIds)
        {
            _metaOpID = compOp.Definition.ID;
            _oldMetaInputs = new List<MetaInput>(compOp.Definition.Inputs);
            _newMetaInputIds = operatorPartIds;
        }

        public void Undo()
        {
            MetaOp.Inputs = _oldMetaInputs;
        }

        public void Do()
        {
            // Rebuild new list with order given by 
            var newList = new List<MetaInput>();
            foreach (var id in _newMetaInputIds)
            {
                foreach (var input in _oldMetaInputs)
                {
                    if (input.ID == id)
                    {
                        newList.Add(input);
                        break;
                    }
                }
            }
            var metaOp = MetaOp;
            metaOp.Inputs = newList;
        }

        private MetaOperator MetaOp => MetaManager.Instance.GetMetaOperator(_metaOpID);

        private readonly List<MetaInput> _oldMetaInputs;

        [JsonProperty]
        private Guid _metaOpID;

        [JsonProperty]
        private List<Guid> _newMetaInputIds;
    }
}
