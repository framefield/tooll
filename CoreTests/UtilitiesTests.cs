// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Framefield.Core;

namespace CoreTests
{
    [TestClass]
    public class UtilitiesTests
    {
        #region ValueFunction tests
        [TestMethod]
        public void CreateValueFunction_CreateTextFunction_ReturnedFunctionEvalsToInputText() {
            var testContext = new OperatorPartContext();
            var testText = "Test1234";
            var func = Utilities.CreateValueFunction(new Text(testText));
            Assert.AreEqual(testText, func.Eval(testContext, new List<OperatorPart>(), 0).Text);
        }

        [TestMethod]
        public void CreateValueFunction_CreateFloatFunction_ReturnedFunctionEvalsToInputFloat() {
            var testContext = new OperatorPartContext();
            var testFloat = 23.0f;
            var func = Utilities.CreateValueFunction(new Float(testFloat));
            Assert.AreEqual(testFloat, func.Eval(testContext, new List<OperatorPart>(), 0).Value);
        }

        [TestMethod]
        public void CreateValueFunction_CreateFloatFunctionWithInputValue_InputValueIsCopiedAndNotReferenced() {
            var testContext = new OperatorPartContext();
            var inputFloatValue = 17.0f;
            var inputFloat = new Float(inputFloatValue);
            var func = Utilities.CreateValueFunction(inputFloat);
            inputFloat.Val = 32.0f;
            Assert.AreEqual(inputFloatValue, func.Eval(testContext, new List<OperatorPart>(), 0).Value);
        }

        #endregion

        #region Swap tests
        [TestMethod]
        public void Swap_Exchange2Values_IntsAreExchanged() {
            var a = 17;
            var b = 23;
            Utilities.Swap(ref a, ref b);
            Assert.AreEqual(23, a);
            Assert.AreEqual(17, b);
        }


        [TestMethod]
        public void Swap_Exchange2Refs_ObjectsAreExchanged() {
            var a = "ref1";
            var b = "ref2";
            Utilities.Swap(ref a, ref b);
            Assert.AreEqual("ref2", a);
            Assert.AreEqual("ref1", b);
        }
        #endregion

        #region Clamp tests
        [TestMethod]
        public void Clamp_WhenValueIsBelowBoundries_ResultIsLowerBound() {
            var lowerBound = 17.0;
            var upperBound = 89.0;
            var value = 0.0;
            value = Utilities.Clamp(value, lowerBound, upperBound);
            Assert.AreEqual(lowerBound, value);
        }

        [TestMethod]
        public void Clamp_WhenValueIsAboveBoundries_ResultIsUpperBound() {
            var lowerBound = 17.0;
            var upperBound = 89.0;
            var value = 129.0;
            value = Utilities.Clamp(value, lowerBound, upperBound);
            Assert.AreEqual(upperBound, value);
        }

        [TestMethod]
        public void Clamp_WhenValueIsWithinBoundries_ResultIsValue() {
            var lowerBound = 17.0;
            var upperBound = 89.0;
            var value = 57.0;
            var result = Utilities.Clamp(value, lowerBound, upperBound);
            Assert.AreEqual(value, result);
        }

        [TestMethod]
        public void Clamp_WhenValueIsLowerBound_ResultIsValue() {
            var lowerBound = 17.0;
            var upperBound = 89.0;
            var value = lowerBound;
            var result = Utilities.Clamp(value, lowerBound, upperBound);
            Assert.AreEqual(value, result);
        }

        [TestMethod]
        public void Clamp_WhenValueIsUpperBound_ResultIsValue() {
            var lowerBound = 17.0;
            var upperBound = 89.0;
            var value = upperBound;
            var result = Utilities.Clamp(value, lowerBound, upperBound);
            Assert.AreEqual(value, result);
        }

        [TestMethod]
        public void Clamp_WhenLowerBoundIsLargerThenUpperBound_ResultIsClampedToLowerBound() {
            var lowerBound = 89.0;
            var upperBound = 17.0;
            var value = 56.0;
            value = Utilities.Clamp(value, lowerBound, upperBound);
            Assert.AreEqual(lowerBound, value);
        }
        #endregion

        #region CharAtToUpper
        [TestMethod]
        public void CharAtToUpper_upper1stChar_1stCharIsUppered() {
            var result = Utilities.CharAtToUpper("min", 0);
            Assert.AreEqual("Min", result);
        }

        [TestMethod]
        public void CharAtToUpper_upper4thChar_4thCharIsUppered() {
            var result = Utilities.CharAtToUpper("posx", 3);
            Assert.AreEqual("posX", result);
        }

        [TestMethod]
        public void CharAtToUpper_negativIndex_returnsInput() {
            var text = "bla";
            var result = Utilities.CharAtToUpper(text, -2);
            Assert.AreEqual(text, result);
        }

        [TestMethod]
        public void CharAtToUpper_indexLargetThanInput_returnsInput() {
            var text = "bla";
            var result = Utilities.CharAtToUpper(text, 20);
            Assert.AreEqual(text, result);
        }
        #endregion

        #region CharAtToLower
        [TestMethod]
        public void CharAtToLower_lower1stChar_1stCharIsLowered() {
            var result = Utilities.CharAtToLower("Min", 0);
            Assert.AreEqual("min", result);
        }

        [TestMethod]
        public void CharAtToLower_lower5thChar_5thCharIsLowered() {
            var result = Utilities.CharAtToLower("hallO", 4);
            Assert.AreEqual("hallo", result);
        }

        [TestMethod]
        public void CharAtToLower_negativIndex_returnsInput() {
            var text = "bla";
            var result = Utilities.CharAtToLower(text, -2);
            Assert.AreEqual(text, result);
        }

        [TestMethod]
        public void CharAtToLower_indexLargetThanInput_returnsInput() {
            var text = "bla";
            var result = Utilities.CharAtToLower(text, 20);
            Assert.AreEqual(text, result);
        }
        #endregion

        #region rad/degree convertion tests
        [TestMethod]
        public void RadToDegree_inputIs0_returns0() {
            Assert.AreEqual(0.0, Utilities.RadToDegree(0.0));
        }

        [TestMethod]
        public void RadToDegree_inputIsPI_returns180() {
            Assert.AreEqual(180.0, Utilities.RadToDegree(Math.PI));
        }

        [TestMethod]
        public void RadToDegree_inputIsMinusPI_returnsMinus180() {
            Assert.AreEqual(-180.0, Utilities.RadToDegree(-Math.PI));
        }

        [TestMethod]
        public void RadToDegree_inputIs2PI_returns360() {
            Assert.AreEqual(360.0, Utilities.RadToDegree(2*Math.PI));
        }

        [TestMethod]
        public void DegreeToRad_inputIs0_returns0() {
            Assert.AreEqual(0.0, Utilities.DegreeToRad(0.0));
        }

        [TestMethod]
        public void DegreeToRad_inputIs180_returnsPI() {
            Assert.AreEqual(Math.PI, Utilities.DegreeToRad(180.0));
        }

        [TestMethod]
        public void DegreeToRad_inputIsMinus180_returnsMinusPI() {
            Assert.AreEqual(-Math.PI, Utilities.DegreeToRad(-180.0));
        }

        [TestMethod]
        public void DegreeToRad_inputIs360_returnsPI() {
            Assert.AreEqual(2.0*Math.PI, Utilities.DegreeToRad(360.0));
        }
        #endregion

        #region AdjustOpPartNameForCode tests
        [TestMethod]
        public void AdjustOpPartNameForCode_InputIsEmpty_returnsEmtpyString() {
            var result = Utilities.AdjustOpPartNameForCode(string.Empty);
            Assert.AreEqual(String.Empty, result);
        }

        [TestMethod]
        [ExpectedException(typeof(System.NullReferenceException))]
        public void AdjustOpPartNameForCode_InputIsNull_ThrowsNullReferenceException() {
            Utilities.AdjustOpPartNameForCode(null);
        }

        [TestMethod]
        public void AdjustOpPartNameForCode_InputHasGroupSeparator_returnsStringWithoutGroupSeparatorAndNextCharCapital() {
            var result = Utilities.AdjustOpPartNameForCode("pos.x");
            Assert.AreEqual("posX", result);
        }
        #endregion

        #region PropertyStasher test
        [TestMethod]
        public void PropertyStasher_pushModifyAndPopViewportAndObjectTWorld_bothAreRestored() {
            var context = new OperatorPartContext();
            var viewport = new SharpDX.ViewportF(0, 0, 320, 200);
            context.Viewport = viewport;

            using (new PropertyStasher<OperatorPartContext>(context, "Viewport", "ObjectTWorld")) {
                context.Viewport = new SharpDX.ViewportF(0, 0, 640, 480);
                context.ObjectTWorld = SharpDX.Matrix.OrthoLH(1.0f, 1.0f, 0.1f, 1.0f);
            }

            Assert.AreEqual(SharpDX.Matrix.Identity, context.ObjectTWorld);
            Assert.AreEqual(viewport, context.Viewport);
        }


        [TestMethod]
        public void PropertyStasher_modifyCascadedProperty_isRestoredToInitialValue() {
            var context = new OperatorPartContext();
            var viewport = new SharpDX.ViewportF(0, 0, 320, 200);
            context.Viewport = viewport;

            using (new PropertyStasher<OperatorPartContext>(context, "Viewport")) {
                var viewport2 = new SharpDX.Viewport(0, 0, 640, 480);
                context.Viewport = viewport2;
                using (new PropertyStasher<OperatorPartContext>(context, "Viewport")) {
                    context.Viewport = new SharpDX.Viewport(0, 0, 1024, 768);
                }
                Assert.AreEqual(viewport2, context.Viewport);
            }

            Assert.AreEqual(viewport, context.Viewport);
        }
        #endregion

        #region CollectAllOperators
        [TestMethod]
        public void CollectAllOperators_SingleOperator_Returns1()
        {
            var metaOp = MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid());
            var opInstance = metaOp.CreateOperator(Guid.NewGuid());
            var output = opInstance.Outputs.First();

            var collectedOperators = new HashSet<Operator>();
            output.CollectAllOperators(collectedOperators);

            Assert.AreEqual(1, collectedOperators.Count);
        }

        [TestMethod]
        public void CollectAllOperators_2NestedOperatorsWithoutConnection_Returns2()
        {
            var metaOp = MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid());
            metaOp.AddOperator(MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid()));
            var opInstance = metaOp.CreateOperator(Guid.NewGuid());
            var output = opInstance.Outputs.First();

            var collectedOperators = new HashSet<Operator>();
            output.CollectAllOperators(collectedOperators);

            Assert.AreEqual(2, collectedOperators.Count);
        }

        [TestMethod]
        public void CollectAllOperators_3NestedWith2EqualOperatorsWithoutConnection_Returns3()
        {
            var metaOp = MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid());
            var nestedMetaOp = MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid());
            metaOp.AddOperator(nestedMetaOp);
            metaOp.AddOperator(nestedMetaOp);
            var opInstance = metaOp.CreateOperator(Guid.NewGuid());
            var output = opInstance.Outputs.First();

            var collectedOperators = new HashSet<Operator>();
            output.CollectAllOperators(collectedOperators);

            Assert.AreEqual(3, collectedOperators.Count);
        }
        #endregion

        #region CollectAllMetaOperators
        [TestMethod]
        public void CollectAllMetaOperators_SingleOperator_Returns1()
        {
            var metaOp = MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid());
            var opInstance = metaOp.CreateOperator(Guid.NewGuid());
            var output = opInstance.Outputs.First();

            var collectedMetaOperators = new HashSet<MetaOperator>();
            output.CollectAllMetaOperators(collectedMetaOperators);

            Assert.AreEqual(1, collectedMetaOperators.Count);
        }

        [TestMethod]
        public void CollectAllMetaOperators_2NestedOperatorsWithoutConnection_Returns2()
        {
            var metaOp = MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid());
            metaOp.AddOperator(MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid()));
            var opInstance = metaOp.CreateOperator(Guid.NewGuid());
            var output = opInstance.Outputs.First();

            var collectedMetaOperators = new HashSet<MetaOperator>();
            output.CollectAllMetaOperators(collectedMetaOperators);

            Assert.AreEqual(2, collectedMetaOperators.Count);
        }

        [TestMethod]
        public void CollectAllMetaOperators_3NestedWith2EqualOperatorsWithoutConnection_Returns2()
        {
            var metaOp = MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid());
            var nestedMetaOp = MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid());
            metaOp.AddOperator(nestedMetaOp);
            metaOp.AddOperator(nestedMetaOp);
            var opInstance = metaOp.CreateOperator(Guid.NewGuid());
            var output = opInstance.Outputs.First();

            var collectedMetaOperators = new HashSet<MetaOperator>();
            output.CollectAllMetaOperators(collectedMetaOperators);

            Assert.AreEqual(2, collectedMetaOperators.Count);
        }
        #endregion
    }
}
