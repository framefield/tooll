// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Linq;
using Framefield.Core;
using Framefield.Core.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace CoreTests.Commands
{
    [TestClass]
    public class RemoveInputCommandTests : InputCommandsTests
    {
        [TestMethod]
        public void RemoveInput_AddAndRemoveInput_OperatorDoesNotContainNewInput()
        {
            const string newInputName = "newTestInput";
            var newInput = new MetaInput(Guid.NewGuid(), newInputName, BasicMetaTypes.FloatMeta, new Float(0.0f), false);
            _operator.Definition.AddInput(newInput);
            var inputToRemove = _operator.Inputs.Find(input => input.Name == newInputName);
            var removeInputCommand = new RemoveInputCommand(_operator, inputToRemove);
            removeInputCommand.Do();
            Assert.IsNull(_operator.Inputs.Find(input => input.Name == newInputName));
        }

        [TestMethod]
        public void RemoveInput_AddAndRemoveInput_OperatorContainsNewInputAfterUndo()
        {
            const string newInputName = "newTestInput";
            var newInput = new MetaInput(Guid.NewGuid(), newInputName, BasicMetaTypes.FloatMeta, new Float(0.0f), false);
            _operator.Definition.AddInput(newInput);
            var inputToRemove = _operator.Inputs.Find(input => input.Name == newInputName);
            var removeInputCommand = new RemoveInputCommand(_operator, inputToRemove);
            removeInputCommand.Do();
            removeInputCommand.Undo();
            Assert.IsNotNull(_operator.Inputs.Find(input => input.Name == newInputName));
        }

        [TestMethod]
        public void RemoveInput_AddAndRemoveInputWithInGoingConnection_ConnectionsAreRestoredAfterUndo()
        {
            const string newInputName = "newTestInput";
            var newInput = new MetaInput(Guid.NewGuid(), newInputName, BasicMetaTypes.FloatMeta, new Float(0.0f), false);
            _operator.Definition.AddInput(newInput);

            var internalOp = _parentOperator.InternalOps.Find(op => op.Definition.ID == _operator.Definition.ID);
            var internalInput = _operator.Definition.Inputs.Find(input => input.Name == newInputName);
            var inputToRemove = _operator.Inputs.Find(input => input.Name == newInputName);

            _operator.Definition.Connections = new[] { new MetaConnection(Guid.Empty, internalInput.ID, Guid.Empty, _operator.Definition.Outputs[0].ID) }.ToList();
            _parentOperator.Definition.Connections = new[] { (new MetaConnection(Guid.Empty, _parentOperator.Definition.Inputs[0].ID, internalOp.ID, internalInput.ID)) }.ToList();

            var removeInputCommand = new RemoveInputCommand(_operator, inputToRemove);
            removeInputCommand.Do();
            removeInputCommand.Undo();

            Assert.IsNotNull(_operator.Definition.Connections.Find(connection => connection.SourceOpPartID == internalInput.ID && connection.TargetOpPartID == _operator.Definition.Outputs[0].ID));
            Assert.IsNotNull(_parentOperator.Definition.Connections.Find(connection => connection.TargetOpID == internalOp.ID && connection.TargetOpPartID == internalInput.ID));
        }

        [TestMethod]
        public void RemoveInput_AddAndRemoveInputSerializeCommand_CommandIsUndoableAfterDeserialisation()
        {
            const string newInputName = "newTestInput";
            var newInput = new MetaInput(Guid.NewGuid(), newInputName, BasicMetaTypes.FloatMeta, new Float(0.0f), false);
            _operator.Definition.AddInput(newInput);
            var inputToRemove = _operator.Inputs.Find(input => input.Name == newInputName);
            var removeInputCommand = new RemoveInputCommand(_operator, inputToRemove);
            removeInputCommand.Do();

            var jsonString = SerializeCommand(removeInputCommand);
            var command = JsonConvert.DeserializeObject<PersistentCommand>(jsonString, _serializerSettings);
            command.Command.Undo();

            Assert.IsNotNull(_operator.Inputs.Find(input => input.Name == newInputName));
        }
    }
}
