// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framefield.Core.Commands
{
    public class UpdateOperatorPartValueFunctionCommand : ICommand
    {
        public string Name { get { return "Set Value"; } }
        public bool IsUndoable { get { return true; } }
        public IValue Value { get; set; }
        public IValue PreviousValue { get { return _previousValue; } }

        public UpdateOperatorPartValueFunctionCommand() { }

        public UpdateOperatorPartValueFunctionCommand(OperatorPart opPart, IValue newValue)
        {
            _isDefaultFuncSet = opPart.IsDefaultFuncSet;
            _previousValue = (opPart.Func as Utilities.ValueFunction).Value;
            Value = newValue;
            _opPartInstanceID = opPart.ID;
            _parentOperatorInstanceID = opPart.Parent.ID;
            _parentOperatorMetaID = opPart.Parent.Definition.ID;
        }

        public void Do()
        {
            var opPart = GetInput();
            opPart.Func = Utilities.CreateValueFunction(Value);          
            opPart.Parent.Parent.Definition.Changed = true;
        }

        public void Undo()
        {
            var input = GetInput();
            if (_isDefaultFuncSet)
            {
                input.SetValueToDefault();
            }
            else
            {
                input.Func = Utilities.CreateValueFunction(_previousValue);
            }
        }


        private OperatorPart GetInput()
        {
            var parentMeta = MetaManager.Instance.GetMetaOperator(_parentOperatorMetaID);
            var parentInstance = parentMeta.GetOperatorInstance(_parentOperatorInstanceID);
            var opPart = (from input in parentInstance.Inputs where input.ID == _opPartInstanceID select input).Single();
            return opPart;
        }

        [JsonProperty]
        private bool _isDefaultFuncSet;
        [JsonProperty]
        private IValue _previousValue;
        [JsonProperty]
        private Guid _parentOperatorMetaID;
        [JsonProperty]
        private Guid _parentOperatorInstanceID;
        [JsonProperty]
        private Guid _opPartInstanceID;

    }
}
