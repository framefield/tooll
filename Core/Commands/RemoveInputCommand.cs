// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Linq;
using Newtonsoft.Json;

namespace Framefield.Core.Commands
{
    public class RemoveInputCommand : ICommand
    {
        public string Name { get; private set; }
        public bool IsUndoable { get { return true; } }

        public RemoveInputCommand() { }

        public RemoveInputCommand(Operator compOp, OperatorPart input)
        {
            Name = "Remove Input Parameter";
            _metaOpID = compOp.Definition.ID;
            _inputID = input.ID;

            bool isHomeOp = compOp.Parent == null;
            _parentID = isHomeOp ? Guid.Empty : compOp.Parent.Definition.ID;

            _metaInputIndex = compOp.Inputs.IndexOf(input);
            var metaInput = compOp.Definition.Inputs[_metaInputIndex];
            _metaInputID = metaInput.ID;
            _metaInputName = metaInput.Name;
            _metaInputDefaultValue = metaInput.DefaultValue;
            _metaOpPartType = metaInput.OpPart.Type;
            _metaInputIsMultiInput = metaInput.IsMultiInput;
            _metaInputRelevance = metaInput.Relevance;

            if (!isHomeOp)
            {
                _connectionsIntoInput = (from connection in compOp.Parent.Definition.Connections
                                         where connection.TargetOpID == compOp.ID
                                         where connection.TargetOpPartID == _inputID
                                         select connection).ToArray();
            }
            _connectionsFromInput = (from connection in compOp.Definition.Connections
                                     where !compOp.Definition.IsBasic // connections from input to code op part (basic op) doesn't need to be restored
                                     where connection.SourceOpPartID == _inputID
                                     select connection).ToArray();
        }

        public void Undo()
        {
            AddRestoredInputToMetaOp();
            RestoreDeletedConnections();
        }

        public void Do()
        {
            var metaOp = MetaManager.Instance.GetMetaOperator(_metaOpID);
            metaOp.RemoveInput(_inputID);
        }

        private void AddRestoredInputToMetaOp()
        {
            var metaOp = MetaManager.Instance.GetMetaOperator(_metaOpID);
            var restoredInput = new MetaInput(_metaInputID, _metaInputName, BasicMetaTypes.GetMetaOperatorPartOf(_metaOpPartType), _metaInputDefaultValue, _metaInputIsMultiInput) { Relevance = _metaInputRelevance };
            metaOp.InsertInput(_metaInputIndex, restoredInput);
        }

        private void RestoreDeletedConnections()
        {
            if (_parentID != Guid.Empty)
            {
                var parent = MetaManager.Instance.GetMetaOperator(_parentID);
                foreach (var metaConnection in _connectionsIntoInput)
                {
                    parent.InsertConnectionAt(metaConnection);
                }
            }

            var metaOp = MetaManager.Instance.GetMetaOperator(_metaOpID);
            foreach (var metaConnection in _connectionsFromInput)
            {
                metaOp.InsertConnectionAt(metaConnection);
            }
        }

        [JsonProperty] 
        private Guid _metaOpID;
        [JsonProperty]
        private Guid _inputID;
        [JsonProperty]
        private Guid _parentID;
        [JsonProperty]
        private MetaConnection[] _connectionsIntoInput;
        [JsonProperty]
        private Guid _metaInputID;
        [JsonProperty]
        private IValue _metaInputDefaultValue;
        [JsonProperty]
        private FunctionType _metaOpPartType;
        [JsonProperty]
        private bool _metaInputIsMultiInput;
        [JsonProperty]
        private string _metaInputName;
        [JsonProperty]
        private int _metaInputIndex;
        [JsonProperty]
        private MetaInput.RelevanceType _metaInputRelevance;
        [JsonProperty]
        private MetaConnection[] _connectionsFromInput;
    }
}
