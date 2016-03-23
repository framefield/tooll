// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using Framefield.Core;
using Framefield.Core.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTests.Commands
{
    [TestClass]
    public class UpdateInputParameterCommandTests : InputCommandsTests
    {
        [TestMethod]
        public void UpdateInputParameter_AddNewInputToOperatorThenChangeInputRelevancy_NewInputHasNewRelevancy()
        {
            const string newInputName = "newTestInput";
            var newInput = new MetaInput(Guid.NewGuid(), newInputName, BasicMetaTypes.FloatMeta, new Float(0.0f), false);
            _operator.Definition.AddInput(newInput);
            var newRelevancy = MetaInput.RelevanceType.Required;
            var changes = new UpdateInputParameterCommand.Entry(newInput) { Relevance = newRelevancy };
            var updateInputCommand = new UpdateInputParameterCommand(_operator, newInput.ID, changes);
            updateInputCommand.Do();

            Assert.IsTrue(_operator.Definition.Inputs.Find(input => input.Name == newInputName).Relevance == newRelevancy);
        }
    }
}
