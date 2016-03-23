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
    public class SetInputAsDefault : ICommand
    {
        public string Name { get { return "Set input as default"; } }
        public bool IsUndoable { get { return true; } }

        public SetInputAsDefault() { }

        public SetInputAsDefault(OperatorPart input)
        {
            var metaInput = input.Parent.GetMetaInput(input);
            _inputMetaID = metaInput.ID;
            _instanceID = input.ID;
            _previousDefaultValue = ((Utilities.ValueFunction) input.DefaultFunc).Value;
            _newDefaultValue = ((Utilities.ValueFunction) input.Func).Value;
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
            inputInstance.DefaultFunc = Utilities.CreateDefaultValueFunction(_previousDefaultValue);
        }

        public void Do()
        {
            var parentMeta = MetaManager.Instance.GetMetaOperator(_parentMetaID);
            var inputMeta = (from input in parentMeta.Inputs
                             where input.ID == _inputMetaID
                             select input).Single();
            inputMeta.DefaultValue = _newDefaultValue;
        }

        [JsonProperty]
        private Guid _inputMetaID;
        [JsonProperty]
        private Guid _parentMetaID;
        [JsonProperty]
        private Guid _parentInstanceID;
        [JsonProperty]
        private Guid _instanceID;
        [JsonProperty]
        [JsonConverter(typeof(JsonIValueConverter))]
        private IValue _previousDefaultValue;
        [JsonProperty]
        [JsonConverter(typeof(JsonIValueConverter))]
        private IValue _newDefaultValue;
    }

}
