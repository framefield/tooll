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
    public class SetFloatValueCommand : ICommand
    {
        public string Name { get { return "Set Float Value"; } }
        public bool IsUndoable { get { return true; } }

        public SetFloatValueCommand() { }

        public SetFloatValueCommand(OperatorPart opPart, float value)
        {
            var valueFunc = opPart.Func as Utilities.ValueFunction;
            var floatValue = valueFunc.Value as Float;
            _previousValue = floatValue.Val;
            _value = value;
            _opPartInstanceID = opPart.ID;
            _opInstanceID = opPart.Parent.ID;
            _parentMetaID = opPart.Parent.Parent.Definition.ID;
        }

        public void Undo()
        {
            var parentMeta = MetaManager.Instance.GetMetaOperator(_parentMetaID);
            var opMeta = parentMeta.Operators[_opInstanceID].Item1;
            var opInstance = opMeta.GetOperatorInstance(_opInstanceID);
            var opPart = (from input in opInstance.Inputs where input.ID == _opPartInstanceID select input).Single();

            opPart.Func = Utilities.CreateValueFunction(new Float(_previousValue));
        }

        public void Do()
        {
            var parentMeta = MetaManager.Instance.GetMetaOperator(_parentMetaID);
            var opMeta = parentMeta.Operators[_opInstanceID].Item1;
            var opInstance = opMeta.GetOperatorInstance(_opInstanceID);
            var opPart = (from input in opInstance.Inputs where input.ID == _opPartInstanceID select input).Single();

            opPart.Func = Utilities.CreateValueFunction(new Float(_value));
        }

        [JsonProperty]
        private float _value;
        [JsonProperty]
        private float _previousValue;
        [JsonProperty]
        private Guid _parentMetaID;
        [JsonProperty]
        private Guid _opInstanceID;
        [JsonProperty]
        private Guid _opPartInstanceID;
    }

}
