// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Linq;
using Newtonsoft.Json;

namespace Framefield.Core.Commands
{
    public class UpdateInputParameterCommand : ICommand
    {
        public string Name { get; private set; }
        public bool IsUndoable => true;

        public class Entry
        {
            public FunctionType FunctionType;
            public string Name;
            public IValue DefaultValue;
            public MetaInput.RelevanceType Relevance;
            public bool IsMultiInput;
            public float Min, Max, Scale;
            public MetaInput.Scaling ScaleType;

            public Entry(MetaInput input)
            {
                FunctionType = input.OpPart.Type;
                Name = input.Name;
                DefaultValue = input.DefaultValue;
                Relevance = input.Relevance;
                IsMultiInput = input.IsMultiInput;
                if (input.OpPart.Type == FunctionType.Float)
                {
                    Min = input.Min;
                    Max = input.Max;
                    Scale = input.Scale;
                    ScaleType = input.ScaleType;
                }
            }
        }

        public UpdateInputParameterCommand() { }

        public UpdateInputParameterCommand(Operator compOp, Guid metaInputID, Entry changes)
        {
            Name = "Update Input Parameter";
            InitPreviousProperties(compOp, metaInputID);
            ChangeEntry = changes;
        }

        private void InitPreviousProperties(Operator compOp, Guid metaInputID)
        {
            _metaOpID = compOp.Definition.ID;
            _inputID = metaInputID;
            var entry = new Entry(GetInputToUpdate());
            _previousEntry = entry;
        }

        private void ApplyEntryToInput(Entry entry)
        {
            MetaInput inputToUpdate = GetInputToUpdate();
            inputToUpdate.OpPart = BasicMetaTypes.GetMetaOperatorPartOf(entry.FunctionType);
            inputToUpdate.Name = entry.Name;
            inputToUpdate.DefaultValue = entry.DefaultValue;
            inputToUpdate.Relevance = entry.Relevance;
            inputToUpdate.IsMultiInput = entry.IsMultiInput;

            if (entry.FunctionType == FunctionType.Float)
            {
                ApplyFloatEntriesToInput(entry);
            }

            MetaOp.UpdateInput(inputToUpdate);
        }

        private void ApplyFloatEntriesToInput(Entry entry)
        {
            var inputToUpdate = GetInputToUpdate();
            inputToUpdate.Min = entry.Min;
            inputToUpdate.Max = entry.Max;
            inputToUpdate.Scale = entry.Scale;
            inputToUpdate.ScaleType = entry.ScaleType;
        }

        public void Undo()
        {
            ApplyEntryToInput(_previousEntry);
        }

        public void Do()
        {
            ApplyEntryToInput(ChangeEntry);
        }

        private MetaInput GetInputToUpdate()
        {
            var inputToUpdate = (from input in MetaOp.Inputs
                                 where input.ID == _inputID
                                 select input).SingleOrDefault();
            return inputToUpdate;
        }

        private MetaOperator MetaOp => MetaManager.Instance.GetMetaOperator(_metaOpID);

        [JsonProperty]
        private Guid _metaOpID;
        [JsonProperty]
        private Guid _inputID;
        [JsonProperty]
        private Entry _previousEntry;
        [JsonProperty]
        public Entry ChangeEntry { get; private set; }
    }
}
