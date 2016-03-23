// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Framefield.Core;
using Framefield.Core.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace CoreTests.Commands
{
    [TestClass]
    public class AddInputCommandTests : InputCommandsTests
    {
        //[TestMethod]
        //public void AddInput_AddNewInputToOperator_OperatorContainsNewInput()
        //{
        //    const string newInputName = "newTestInput";
        //    var addInputCommand = new AddInputCommand(_operator, FunctionType.Scene, newInputName);
        //    addInputCommand.Do();
        //    Assert.IsNotNull(_operator.Inputs.Find(input => input.Name == newInputName));
        //}

        //[TestMethod]
        //public void AddInput_AddNewInputToOperator_OperatorDoesNotContainNewInputAfterUndo()
        //{
        //    const string newInputName = "newTestInput";
        //    var addInputCommand = new AddInputCommand(_operator, FunctionType.Scene, newInputName);
        //    addInputCommand.Do();
        //    addInputCommand.Undo();
        //    Assert.IsNull(_operator.Inputs.Find(input => input.Name == newInputName));
        //}

        //[TestMethod]
        //public void AddInput_AddNewInputToOperator_UndoableAfterDeserialisation()
        //{
        //    const string newInputName = "newTestInput";
        //    var addInputCommand = new AddInputCommand(_operator, FunctionType.Scene, newInputName);
        //    addInputCommand.Do();

        //    var jsonString = SerializeCommand(addInputCommand);
        //    var command = JsonConvert.DeserializeObject<PersistentCommand>(jsonString, _serializerSettings);
        //    command.Command.Undo();

        //    Assert.IsNull(_operator.Inputs.Find(input => input.Name == newInputName));
        //}
    }
}
