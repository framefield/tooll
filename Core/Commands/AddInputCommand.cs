// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Framefield.Core.Commands
{
    public class AddInputCommand : ICommand
    {
        public string Name { get { return "Add Input Parameter"; } }
        public bool IsUndoable { get { return true;} }

        public AddInputCommand() { }

        private AddInputCommand(Operator compOp, FunctionType funcType, string name, Guid id)
        {
            _metaOpID = compOp.Definition.ID;
            _inputName = name;
            _funcType = funcType;
            _newInputID = id;
            _applyProperties = false;
        }

        public AddInputCommand(Operator compOp, MetaInput inputDefinition)
            : this(compOp, inputDefinition.OpPart.Type, inputDefinition.Name, inputDefinition.ID)
        {
            _isMultiInput = inputDefinition.IsMultiInput;
            _relevance = inputDefinition.Relevance;
            _description = inputDefinition.Description;
            _min = inputDefinition.Min;
            _max = inputDefinition.Max;
            _defaultValue = inputDefinition.DefaultValue;
            _scale = inputDefinition.Scale;
            _scaleType = inputDefinition.ScaleType;
            _enumValues = new List<MetaInput.EnumEntry>(inputDefinition.EnumValues);
            _applyProperties = true;
        }

        private MetaInput CreateMetaInput()
        {
            MetaOperatorPart opPart;
            switch (_funcType)
            {
                case FunctionType.Dynamic:
                    opPart = BasicMetaTypes.DynamicMeta;
                    break;

                case FunctionType.Float:
                    opPart = BasicMetaTypes.FloatMeta;
                    break;

                case FunctionType.Generic:
                    opPart = BasicMetaTypes.GenericMeta;
                    break;

                case FunctionType.Image:
                    opPart = BasicMetaTypes.ImageMeta;
                    break;

                case FunctionType.Scene:
                    opPart = BasicMetaTypes.SceneMeta;
                    break;

                case FunctionType.Text:
                    opPart = BasicMetaTypes.TextMeta;
                    break;

                case FunctionType.Mesh:
                    opPart = BasicMetaTypes.MeshMeta;
                    break;

                default:
                    return null;
            }
            return new MetaInput(_newInputID, _inputName, opPart, _defaultValue, false);
        }

        public void Undo()
        {
            MetaOp.RemoveInput(_newInputID);
        }

        public void Do()
        {
            var inputDefinition = CreateMetaInput();
            if (_applyProperties)
            {
                inputDefinition.IsMultiInput = _isMultiInput;
                inputDefinition.Relevance = _relevance;
                inputDefinition.Description = _description;
                inputDefinition.Min = _min;
                inputDefinition.Max = _max;
                inputDefinition.DefaultValue = _defaultValue;
                inputDefinition.Scale = _scale;
                inputDefinition.ScaleType = _scaleType;
                inputDefinition.EnumValues = new List<MetaInput.EnumEntry>(_enumValues);
            }
            MetaOp.AddInput(inputDefinition);
        }

        private MetaOperator MetaOp { get { return MetaManager.Instance.GetMetaOperator(_metaOpID); } }

        [JsonProperty]
        private Guid _metaOpID;
        [JsonProperty]
        private string _inputName;
        [JsonProperty]
        private Guid _newInputID;
        [JsonProperty]
        private FunctionType _funcType;

        [JsonProperty]
        private bool _isMultiInput;
        [JsonProperty]
        private MetaInput.RelevanceType _relevance;
        [JsonProperty]
        private string _description;

        // float stuff
        [JsonProperty]
        private float _min;
        [JsonProperty]
        private float _max;
        [JsonProperty]
        private float _scale;
        [JsonProperty]
        private MetaInput.Scaling _scaleType;
        [JsonProperty]
        private List<MetaInput.EnumEntry> _enumValues;
        [JsonProperty]
        private bool _applyProperties;
        [JsonProperty]
        private IValue _defaultValue;
    }
}
