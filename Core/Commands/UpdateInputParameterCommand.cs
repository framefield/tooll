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
        public bool IsUndoable { get { return true; } }

        public class Entry
        {
            public IValue DefaultValue;
            public MetaInput.RelevanceType Relevance;
            public bool IsMultiInput;
            public float Min, Max, Scale;
            public MetaInput.Scaling ScaleType;

            public Entry(MetaInput input)
            {
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
            _changeEntry = changes;
        }

        private void InitPreviousProperties(Operator compOp, Guid metaInputID)
        {
            _metaOpID = compOp.Definition.ID;
            _inputID = metaInputID;
            var entry = new Entry(InputToUpdate);
            _previousEntry = entry;
        }

        private void ApplyEntryToInput(Entry entry)
        {
            InputToUpdate.DefaultValue = entry.DefaultValue;
            InputToUpdate.Relevance = entry.Relevance;
            InputToUpdate.IsMultiInput = entry.IsMultiInput;
            if (InputToUpdate.OpPart.Type == FunctionType.Float)
            {
                ApplyFloatEntriesToInput(entry);
            }
        }

        private void ApplyFloatEntriesToInput(Entry entry)
        {
            InputToUpdate.Min = entry.Min;
            InputToUpdate.Max = entry.Max;
            InputToUpdate.Scale = entry.Scale;
            InputToUpdate.ScaleType = entry.ScaleType;
        }

        public void Undo()
        {
            ApplyEntryToInput(_previousEntry);
        }

        public void Do()
        {
            ApplyEntryToInput(_changeEntry);    
        }

        private MetaInput InputToUpdate
        {
            get
            {
                var inputToUpdate = (from input in MetaOp.Inputs
                                     where input.ID == _inputID
                                     select input).SingleOrDefault();
                return inputToUpdate;
            }
        }

        private MetaOperator MetaOp { get { return MetaManager.Instance.GetMetaOperator(_metaOpID); } }

        [JsonProperty]
        private Guid _metaOpID;
        [JsonProperty]
        private Guid _inputID;
        [JsonProperty]
        private Entry _previousEntry;
        [JsonProperty]
        private Entry _changeEntry;
    }
}
