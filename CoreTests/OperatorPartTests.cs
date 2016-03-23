// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Framefield.Core;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTests
{
    [TestClass]
    public class OperatorPartTests
    {
        private OperatorPart createOpPartWith2Inputs() {
            var testOpPart = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Float(0.0f)), true);
            var input1 = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Float(5.0f)), false);
            var input2 = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Float(10.0f)), false);
            
            testOpPart.AppendConnection(input1);
            testOpPart.AppendConnection(input2);

            return testOpPart;
        }

        private OperatorPart insertOpWith50AtIndex(int index) {
            var testOpPart = createOpPartWith2Inputs();
            var inputMiddle = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Float(50.0f)), false);
            testOpPart.InsertConnectionAt(inputMiddle, index);
            return testOpPart;
        }

        #region InsertConnectionAt tests
        [TestMethod]
        public void InsertConnectionAt_InsertAtIndex1ToTwoExistingInputs_inputHaveLength3() {
            var testOpPart = insertOpWith50AtIndex(1);
            Assert.AreEqual(3, testOpPart.Connections.Count);
        }

        [TestMethod]
        public void InsertConnectionAt_InsertAtIndex1ToTwoExitstingInput_inputIsInMiddle() {
            var testOpPart = insertOpWith50AtIndex(1);
            var context = new OperatorPartContext();
            Assert.AreEqual(50.0f, testOpPart.Connections[1].Eval(context).Value);
        }

        [TestMethod]
        public void InsertConnectionAt_InsertAtIndex1ToTwoExitstingInput_input1StaysAtFront() {
            var testOpPart = insertOpWith50AtIndex(1);
            var context = new OperatorPartContext();
            Assert.AreEqual(5.0f, testOpPart.Connections[0].Eval(context).Value);
        }

        [TestMethod]
        public void InsertConnectionAt_InsertAtIndex1ToTwoExitstingInput_input2IsNowAtEnd() {
            var testOpPart = insertOpWith50AtIndex(1);
            var context = new OperatorPartContext();
            Assert.AreEqual(10.0f, testOpPart.Connections[2].Eval(context).Value);
        }

        [TestMethod]
        public void InsertConnectionAt_InsertAtIndex2ToTwoExitstingInput_inputHaveLength3() {
            var testOpPart = insertOpWith50AtIndex(2);
            Assert.AreEqual(3, testOpPart.Connections.Count);
        }

        [TestMethod]
        public void InsertConnectionAt_InsertAtIndex2ToTwoExitstingInput_inputIsAtIndex2() {
            var testOpPart = insertOpWith50AtIndex(2);
            var context = new OperatorPartContext();
            Assert.AreEqual(50.0f, testOpPart.Connections[2].Eval(context).Value);
        }

        [TestMethod]
        public void InsertConnectionAt_InsertAtIndex2ToTwoExitstingInput_input1StaysAtFront() {
            var testOpPart = insertOpWith50AtIndex(2);
            var context = new OperatorPartContext();
            Assert.AreEqual(5.0f, testOpPart.Connections[0].Eval(context).Value);
        }

        [TestMethod]
        public void InsertConnectionAt_InsertAtIndex2ToTwoExitstingInput_input2IsNowAtMiddle() {
            var testOpPart = insertOpWith50AtIndex(2);
            var context = new OperatorPartContext();
            Assert.AreEqual(10.0f, testOpPart.Connections[1].Eval(context).Value);
        }

        [TestMethod]
        [ExpectedException(typeof(System.Exception))]
        public void InsertConnectionAt_Insert2ConnectionsToNonMultipleInput_throws() {
            var testOpPart = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Float(0.0f)), false);
            var input = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Float(10.0f)), false);
            testOpPart.InsertConnectionAt(input, 0);
            testOpPart.InsertConnectionAt(input, 1);
        }

        [TestMethod]
        [ExpectedException(typeof(System.Exception))]
        public void InsertConnectionAt_InsertConnectionsAtNegativeIndex_throws() {
            var testOpPart = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Float(0.0f)), false);
            var input = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Float(10.0f)), false);
            testOpPart.InsertConnectionAt(input, -1);
        }

        [TestMethod]
        [ExpectedException(typeof(System.Exception))]
        public void InsertConnectionAt_InsertConnectionsAtIndexGreaterThanConnectionsLength_throws() {
            var testOpPart = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Float(0.0f)), false);
            var input = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Float(10.0f)), false);
            testOpPart.InsertConnectionAt(input, 2);
        }
        #endregion

        #region ReplaceConnectionAt tests
        [TestMethod]
        public void ReplaceConnectionAt_ReplaceInputAt1With3ExistingInputs_inputsStillHaveLength2() {
            var opPart = insertOpWith50AtIndex(1);
            var newInput = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Float(17.0f)), false);
            opPart.ReplaceConnectionAt(newInput, 1);
            Assert.AreEqual(3, opPart.Connections.Count);
        }

        [TestMethod]
        public void ReplaceConnectionAt_ReplaceInputAt1With3ExistingInputs_newInputsIsAtIndex1() {
            var opPart = insertOpWith50AtIndex(1);
            var newInput = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Float(17.0f)), false);
            opPart.ReplaceConnectionAt(newInput, 1);
            Assert.AreEqual(newInput.ID, opPart.Connections[1].ID);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ReplaceConnectionAt_AtNegativeIndex_throws() {
            var testOpPart = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Float(0.0f)), false);
            var input = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Float(10.0f)), false);
            testOpPart.ReplaceConnectionAt(input, -1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ReplaceConnectionAt_AtIndexGreaterThanConnectionsLength_throws() {
            var testOpPart = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Float(0.0f)), false);
            var input = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Float(10.0f)), false);
            testOpPart.AppendConnection(input);
            testOpPart.ReplaceConnectionAt(input, 1);
        }
        #endregion

        #region RemoveConnectionAt tests
        [TestMethod]
        public void RemoveConnectionAt_FirstOf2Connections_connectionRemoved() {
            var testOpPart = createOpPartWith2Inputs();
            var lastConnectedOpPartID = testOpPart.Connections[1].ID;

            testOpPart.RemoveConnectionAt(0);

            Assert.AreEqual(1, testOpPart.Connections.Count);
            Assert.AreEqual(lastConnectedOpPartID, testOpPart.Connections[0].ID);
        }

        [TestMethod]
        public void RemoveConnectionAt_LastOf2Connections_connectionRemoved() {
            var testOpPart = createOpPartWith2Inputs();
            var firstConnectedOpPartID = testOpPart.Connections[0].ID;

            testOpPart.RemoveConnectionAt(1);

            Assert.AreEqual(1, testOpPart.Connections.Count);
            Assert.AreEqual(firstConnectedOpPartID, testOpPart.Connections[0].ID);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void RemoveConnectionAt_NegativeIndex_throws() {
            var testOpPart = createOpPartWith2Inputs();
            testOpPart.RemoveConnectionAt(-1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void RemoveConnectionAt_IndexEqualsConnectionsLength_throws() {
            var testOpPart = createOpPartWith2Inputs();
            testOpPart.RemoveConnectionAt(testOpPart.Connections.Count);
        }
        #endregion

        #region Type tests
        [TestMethod]
        public void Type_InitialTypeWithGeneric_typeIsGeneric() {
            var testOpPart = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Generic()), false);
            Assert.AreEqual(FunctionType.Generic, testOpPart.Type);
        }

        [TestMethod]
        public void Type_GenericTypeWithFloatInput_TypeIsFloat() {
            var testOpPart = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Generic()), false);
            var floatInput = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Float(5.0f)), false);
            var textInput = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Text("hallo")), false);
            
            testOpPart.AppendConnection(floatInput);
            Assert.AreEqual(FunctionType.Float, testOpPart.Type);
        }

        [TestMethod]
        public void Type_FloatTypeWithTextInput_TypeIsFloat() {
            var testOpPart = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Float(5.0f)), false);
            var textInput = Utilities.CreateValueOpPart(Guid.NewGuid(), Utilities.CreateDefaultValueFunction(new Text("hallo")), false);

            testOpPart.AppendConnection(textInput);
            Assert.AreEqual(FunctionType.Float, testOpPart.Type);
        }

        [TestMethod]
        public void Type_GenericTypeWithChangingInputType_OutputGetsDisconnected() {
            var parentOp = Utilities.CreateEmptyOperator();
            var genericOp = MetaOperatorTests.CreateGenericMultiInputMetaOperator().CreateOperator(Guid.NewGuid());
            var floatOutputOp = MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid()).CreateOperator(Guid.NewGuid());
            var floatInputOp = MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid()).CreateOperator(Guid.NewGuid());
            var textInputOp = MetaOperatorTests.CreateTextMetaOperator().CreateOperator(Guid.NewGuid());

            parentOp.AddOperator(genericOp);
            parentOp.AddOperator(floatOutputOp);
            parentOp.AddOperator(floatInputOp);
            parentOp.AddOperator(textInputOp);

            parentOp.InsertConnectionAt(new Connection(floatInputOp, floatInputOp.Outputs[0], genericOp, genericOp.Inputs[0], 0));
            parentOp.InsertConnectionAt(new Connection(genericOp, genericOp.Outputs[0], floatOutputOp, floatOutputOp.Inputs[0], 0));
            parentOp.ReplaceConnectionAt(new Connection(textInputOp, textInputOp.Inputs[0], genericOp, genericOp.Inputs[0], 0));

            Assert.AreEqual(0, floatOutputOp.Inputs[0].Connections.Count());
        }

        #endregion
    }
}
