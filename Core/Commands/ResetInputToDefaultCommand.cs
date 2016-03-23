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

    [JsonObject(MemberSerialization.OptIn)]
    public class ResetInputToDefault : ICommand
    {
        public string Name { get { return "Reset input to default"; } }
        public bool IsUndoable { get { return true; } }

        public ResetInputToDefault() { }

        public ResetInputToDefault(OperatorPart input)
        {
            _instanceID = input.ID;
            _valueBeforeReset = ((Utilities.ValueFunction) input.Func).Value;
            _parentInstanceID = input.Parent.ID;
            _parentMetaID = input.Parent.Definition.ID;
        }

        public void Undo()
        {
            var parentMeta = MetaManager.Instance.GetMetaOperator(_parentMetaID);
            var parentInstance = parentMeta.GetOperatorInstance(_parentInstanceID);
            var inputInstance = (from input in parentInstance.Inputs
                                 where input.ID == _instanceID
                                 select input).Single();
            inputInstance.SetValue(_valueBeforeReset);
        }

        public void Do()
        {
            var parentMeta = MetaManager.Instance.GetMetaOperator(_parentMetaID);
            var parentInstance = parentMeta.GetOperatorInstance(_parentInstanceID);
            var inputInstance = (from input in parentInstance.Inputs
                                 where input.ID == _instanceID
                                 select input).Single();
            inputInstance.SetValueToDefault();
        }

        [JsonProperty]
        private Guid _parentMetaID;
        [JsonProperty]
        private Guid _parentInstanceID;
        [JsonProperty]
        private Guid _instanceID;
        [JsonProperty]
        [JsonConverter(typeof(JsonIValueConverter))]
        private IValue _valueBeforeReset;
    }

}
