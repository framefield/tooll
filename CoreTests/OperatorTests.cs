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
    public class OperatorTests
    {
        public static MetaOperator CreateMetaFloatWithoutConnections() {
            var metaOp = MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid());
            metaOp.RemoveConnection(new MetaConnection(Guid.Empty, metaOp.Inputs[0].ID, Guid.Empty, metaOp.Outputs[0].ID), 0);
            Assert.AreEqual(0, metaOp.Connections.Count);
            return metaOp;
        }

        #region Connection tests
        [TestMethod]
        public void InsertConnectionAt_Index0IntoEmptyConnections_connectionInserted() {
            var metaOp = CreateMetaFloatWithoutConnections();
            var op = metaOp.CreateOperator(Guid.NewGuid());
            op.InsertConnectionAt(new Connection(null, op.Inputs[0], null, op.Outputs[0], 0));
            Assert.AreEqual(1, op.Connections.Count());
        }

        [TestMethod]
        public void InsertConnectionAt_Index0IntoEmptyConnections_2ndInstanceIsAlsoUpdated() {
            var metaOp = CreateMetaFloatWithoutConnections();
            var op1 = metaOp.CreateOperator(Guid.NewGuid());
            var op2 = metaOp.CreateOperator(Guid.NewGuid());
            op1.InsertConnectionAt(new Connection(null, op1.Inputs[0], null, op1.Outputs[0], 0));
            Assert.AreEqual(1, op2.Connections.Count());
        }

        [TestMethod]
        [ExpectedException(typeof(System.Exception))]
        public void InsertConnectionAt_Inserting2ndConnectionToSingleInput_throwsException() {
            var metaOp = CreateMetaFloatWithoutConnections();
            var op = metaOp.CreateOperator(Guid.NewGuid());
            op.InsertConnectionAt(new Connection(null, op.Inputs[0], null, op.Outputs[0], 0));
            op.InsertConnectionAt(new Connection(null, op.Inputs[0], null, op.Outputs[0], 1));
        }

        [TestMethod]
        public void InsertConnectionAt_Inserting2ndConnectionAtIndex0ToMultiInput_connectionIsInsertedAtFront() {
            var metaCompOp = new MetaOperator(Guid.NewGuid());
            metaCompOp.AddOperator(MetaOperatorTests.CreateGenericMultiInputMetaOperator());
            metaCompOp.AddOperator(MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid()));
            metaCompOp.AddOperator(MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid()));
            Operator op = metaCompOp.CreateOperator(Guid.NewGuid());
            Operator multiply = op.InternalOps[0];
            Operator float1 = op.InternalOps[1];
            Operator float2 = op.InternalOps[2];

            op.InsertConnectionAt(new Connection(float1, float1.Outputs[0], multiply, multiply.Inputs[0], 0));
            op.InsertConnectionAt(new Connection(float2, float2.Outputs[0], multiply, multiply.Inputs[0], 0));

            Assert.AreEqual(multiply.Inputs[0].Connections[0], float2.Outputs[0]);
        }

        [TestMethod]
        public void RemoveOperator_SourceOperatorWithConnection_operatorAndConnectionRemoved() {
            var metaCompOp = new MetaOperator(Guid.NewGuid());
            metaCompOp.AddOperator(MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid()));
            metaCompOp.AddOperator(MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid()));
            Operator op = metaCompOp.CreateOperator(Guid.NewGuid());
            Operator float1 = op.InternalOps[0];
            Operator float2 = op.InternalOps[1];

            op.InsertConnectionAt(new Connection(float1, float1.Outputs[0], float2, float2.Inputs[0], 0));
            Assert.AreEqual(float1.Outputs[0], float2.Inputs[0].Connections[0]);

            List<System.EventArgs> eventArgs = new List<System.EventArgs>();
            op.ConnectionRemovedEvent += (o, e) => { eventArgs.Add(e); };
            op.OperatorRemovedEvent += (o, e) => { eventArgs.Add(e); };

            op.RemoveOperator(float1);

            Assert.AreEqual(2, eventArgs.Count);
            var connectionChangedEventArgs = eventArgs[0] as ConnectionChangedEventArgs;
            Assert.AreNotEqual(null, connectionChangedEventArgs);
            Assert.AreEqual(float1, connectionChangedEventArgs.Connection.SourceOp);
            Assert.AreEqual(float1.Outputs[0], connectionChangedEventArgs.Connection.SourceOpPart);
            Assert.AreEqual(float2, connectionChangedEventArgs.Connection.TargetOp);
            Assert.AreEqual(float2.Inputs[0], connectionChangedEventArgs.Connection.TargetOpPart);
            Assert.AreNotEqual(null, eventArgs[1] as OperatorChangedEventArgs);
            Assert.AreEqual(1, op.InternalOps.Count);
            Assert.AreEqual(float2, op.InternalOps[0]);
            Assert.AreEqual(0, float2.Inputs[0].Connections.Count);
        }

        [TestMethod]
        public void RemoveOperator_TargetOperatorWithConnection_operatorAndConnectionRemoved() {
            var metaCompOp = new MetaOperator(Guid.NewGuid());
            metaCompOp.AddOperator(MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid()));
            metaCompOp.AddOperator(MetaOperatorTests.CreateFloatMetaOperator(Guid.NewGuid()));
            Operator op = metaCompOp.CreateOperator(Guid.NewGuid());
            Operator float1 = op.InternalOps[0];
            Operator float2 = op.InternalOps[1];

            op.InsertConnectionAt(new Connection(float1, float1.Outputs[0], float2, float2.Inputs[0], 0));
            Assert.AreEqual(float1.Outputs[0], float2.Inputs[0].Connections[0]);

            List<System.EventArgs> eventArgs = new List<System.EventArgs>();
            op.ConnectionRemovedEvent += (o, e) => { eventArgs.Add(e); };
            op.OperatorRemovedEvent += (o, e) => { eventArgs.Add(e); };

            op.RemoveOperator(float2);

            Assert.AreEqual(2, eventArgs.Count);
            var connectionChangedEventArgs = eventArgs[0] as ConnectionChangedEventArgs;
            Assert.AreNotEqual(null, connectionChangedEventArgs);
            Assert.AreEqual(float1, connectionChangedEventArgs.Connection.SourceOp);
            Assert.AreEqual(float1.Outputs[0], connectionChangedEventArgs.Connection.SourceOpPart);
            Assert.AreEqual(float2, connectionChangedEventArgs.Connection.TargetOp);
            Assert.AreEqual(float2.Inputs[0], connectionChangedEventArgs.Connection.TargetOpPart);
            Assert.AreNotEqual(null, eventArgs[1] as OperatorChangedEventArgs);
            Assert.AreEqual(1, op.InternalOps.Count);
            Assert.AreEqual(float1, op.InternalOps[0]);
        }
        #endregion

        #region outputs
        [TestMethod]
        public void Constructor_CreateOperatorWithOneOutput_OutoutHasIndex0()
        {
            var metaOp = CreateMetaFloatWithoutConnections();
            var op = metaOp.CreateOperator(Guid.NewGuid());
            Assert.AreEqual(0, op.Outputs[0].Func.EvaluationIndex);
        }

        [TestMethod]
        public void AddOutput_ToOperatorWithOneOutput_TwoOutputsWithIndices0And1()
        {
            var metaOp = CreateMetaFloatWithoutConnections();
            var op = metaOp.CreateOperator(Guid.NewGuid());
            metaOp.AddOutput(new MetaOutput(Guid.NewGuid(), "Output2", BasicMetaTypes.FloatMeta));
            Assert.AreEqual(2, op.Outputs.Count);
            Assert.AreEqual(0, op.Outputs[0].Func.EvaluationIndex);
            Assert.AreEqual(1, op.Outputs[1].Func.EvaluationIndex);
        }

        [TestMethod]
        public void RemoveOutput_FromOperatorWith2Outputs_OneOutputWithIndex0()
        {
            var metaOp = CreateMetaFloatWithoutConnections();
            var op = metaOp.CreateOperator(Guid.NewGuid());
            metaOp.AddOutput(new MetaOutput(Guid.NewGuid(), "Output2", BasicMetaTypes.FloatMeta));
            metaOp.RemoveOutput(metaOp.Outputs[0].ID);
            Assert.AreEqual(1, op.Outputs.Count);
            Assert.AreEqual(0, op.Outputs[0].Func.EvaluationIndex);
        }

        [TestMethod]
        public void InsertOutput_ToOperatorWithOneOutput_TwoOutputsWithIndices0And1()
        {
            var metaOp = CreateMetaFloatWithoutConnections();
            var op = metaOp.CreateOperator(Guid.NewGuid());
            metaOp.InsertOutput(0, new MetaOutput(Guid.NewGuid(), "Output2", BasicMetaTypes.FloatMeta));
            Assert.AreEqual(2, op.Outputs.Count);
            Assert.AreEqual(0, op.Outputs[0].Func.EvaluationIndex);
            Assert.AreEqual(1, op.Outputs[1].Func.EvaluationIndex);
        }

        // test passes everywhere but not on buildserver
//        [TestMethod]
//        public void OutputEvaluation_OfOperatorWithTwoOutput_OutputValuesAreCrossconnectedInputValues()
//        {
//            const float inputValue1 = 10.0f;
//            const float inputValue2 = 7.0f;
//            var metaOp = MetaOperatorTests.CreateSubMetaOp(inputValue1, inputValue2);
//            metaOp.AddOutput(new MetaOutput(Guid.NewGuid(), "Output2", BasicMetaTypes.FloatMeta));
//            var opPart = metaOp.OperatorParts[0].Item2;
//            var script = @"using System;
//                           using System.Collections.Generic;
//                           namespace Framefield.Core.IDca9a3a0e_c1c7_42b6_a0e5_cdb4c61d0b18
//                           {
//                               public class Class_Sub : OperatorPart.Function
//                               {
//                                   public override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs, int outputIdx) 
//                                   {
//                                        context.Value = outputIdx == 1 ? inputs[0].Eval(context).Value : inputs[1].Eval(context).Value;
//                                        return context;
//                                   }
//                               }
//                           }";
//
//            opPart.Script = script;
//            opPart.Compile();
//
//            var op = metaOp.CreateOperator(Guid.NewGuid());
//
//            Assert.AreEqual(inputValue2, op.Outputs[0].Eval(new OperatorPartContext()).Value);
//            Assert.AreEqual(inputValue1, op.Outputs[1].Eval(new OperatorPartContext()).Value);
//        }

        #endregion
    }
}
