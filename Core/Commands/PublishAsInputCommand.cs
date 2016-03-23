// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Framefield.Core.Commands
{
    public class PublishAsInputCommand : ICommand
    {
        public string Name { get { return "Publish As Input"; } }
        public bool IsUndoable { get { return true; } }
        
        public PublishAsInputCommand() { }

        public PublishAsInputCommand(OperatorPart inputToPublish, string newName)
        {
            _inputToPublishID = inputToPublish.ID;
            _operatorID = inputToPublish.Parent.ID;
            _compOpMetaID = inputToPublish.Parent.Parent.Definition.ID;
            _compOpID = inputToPublish.Parent.Parent.ID;
            _newName = newName;
        }

        public void Undo()
        {
            _commands.Reverse();
            _commands.ForEach(c => c.Undo());
        }

        public void Do()
        {
            var metaInput = InputParent.GetMetaInput(InputToPublish);
            var newMetaInput = metaInput.Clone();
            newMetaInput.Name = _newName;

            var addInputCommand = new AddInputCommand(CompositionOperator, newMetaInput);
            addInputCommand.Do();

            var opPartToUpdate = CompositionOperator.Inputs[CompositionOperator.Inputs.Count - 1];
            var func = InputToPublish.Func.Clone() as Utilities.ValueFunction;
            var updateFuncCommand = new UpdateOperatorPartValueFunctionCommand(opPartToUpdate, func.Value);
            updateFuncCommand.Do();

            var connection = new MetaConnection(Guid.Empty, newMetaInput.ID, InputToPublish.Parent.ID, metaInput.ID);
            var insertConnectionCommand = new InsertConnectionCommand(CompositionOperator.Definition, connection, 0);
            insertConnectionCommand.Do();

            _commands.Clear();
            _commands.AddRange(new ICommand[] {addInputCommand, updateFuncCommand, insertConnectionCommand});
        }

        private List<ICommand> _commands = new List<ICommand>();

        private MetaOperator MetaCompositionOperator { get { return MetaManager.Instance.GetMetaOperator(_compOpMetaID); } }
        private Operator CompositionOperator { get { return MetaCompositionOperator.GetOperatorInstance(_compOpID); } }
        private Operator InputParent { get { return CompositionOperator.InternalOps.Find(op => op.ID == _operatorID); } }
        private OperatorPart InputToPublish { get { return InputParent.Inputs.Find(input => input.ID == _inputToPublishID); } }

        [JsonProperty]
        private Guid _inputToPublishID;
        [JsonProperty]
        private Guid _operatorID;
        [JsonProperty]
        private Guid _compOpMetaID;
        [JsonProperty]
        private Guid _compOpID;
        [JsonProperty]
        private string _newName;
    }
}
