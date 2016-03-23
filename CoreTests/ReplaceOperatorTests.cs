// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using Framefield.Core;
using Framefield.Core.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTests
{
    [TestClass]
    public class ReplaceOperatorTests
    {
        [TestMethod]
        public void Replace_OperatorWithoutConnectionsWithOperatorWithoutConnections_NewOpHasInternalIdOfOldOp()
        {
            var internalOldID = Guid.NewGuid();
            var floatMetaOld = OperatorTests.CreateMetaFloatWithoutConnections();
            var floatMetaNew = OperatorTests.CreateMetaFloatWithoutConnections();

            var metacompositionOp = new MetaOperator(Guid.NewGuid());
            metacompositionOp.AddOperator(floatMetaOld, internalOldID);

            MetaManager.Instance.AddMetaOperator(metacompositionOp.ID, metacompositionOp);
            MetaManager.Instance.AddMetaOperator(floatMetaNew.ID, floatMetaNew);
            MetaManager.Instance.AddMetaOperator(floatMetaOld.ID, floatMetaOld);

            var replaceCommand = new ReplaceOperatorCommand(metacompositionOp, floatMetaOld, floatMetaNew, internalOldID);
            replaceCommand.Do();

            Assert.AreEqual(metacompositionOp.Operators.Count, 1);
            Assert.IsFalse(metacompositionOp.Operators.ContainsKey(internalOldID));
            Assert.AreEqual(metacompositionOp.Operators.Values.Count(value => value.Item1 == floatMetaNew), 1);
            Assert.AreEqual(metacompositionOp.Operators.Values.Count(value => value.Item1 == floatMetaOld), 0);

            MetaManager.Instance.RemoveMetaOperator(metacompositionOp.ID);
            MetaManager.Instance.RemoveMetaOperator(floatMetaNew.ID);
            MetaManager.Instance.RemoveMetaOperator(floatMetaOld.ID);
        }

        [TestMethod]
        public void Replace_OperatorWith1OutWithOperatorWith1Out_ConnectionHasSameTargetAndNewOpAsSource()
        {
            var internalTargetID = Guid.NewGuid();
            var targetOpPartID = Guid.NewGuid();
            var sourceOpPartID = Guid.NewGuid();
            var connectionTargetOp = new MetaOperator(internalTargetID);

            var floatInput = new MetaInput(targetOpPartID, "targetInput", new MetaOperatorPart(Guid.NewGuid()), new Float(5.0f), false);
            connectionTargetOp.AddInput(floatInput);

            var internalOldID = Guid.NewGuid();
            var floatMetaOld = OperatorTests.CreateMetaFloatWithoutConnections();
            var floatOutput = new MetaOutput(sourceOpPartID, "sourceOutputOld", new MetaOperatorPart(Guid.NewGuid()));
            floatMetaOld.AddOutput(floatOutput);

            var floatMetaNew = OperatorTests.CreateMetaFloatWithoutConnections();
            floatMetaNew.AddOutput(new MetaOutput(Guid.NewGuid(), "sourceOutputNew", new MetaOperatorPart(Guid.NewGuid())));

            var metacompositionOp = new MetaOperator(Guid.NewGuid());
            metacompositionOp.AddOperator(connectionTargetOp);
            metacompositionOp.AddOperator(floatMetaOld, internalOldID);
            metacompositionOp.Connections = new[]
                                            {
                                                new MetaConnection(internalOldID, sourceOpPartID, internalTargetID, targetOpPartID)
                                            }.ToList();

            MetaManager.Instance.AddMetaOperator(floatMetaOld.ID, floatMetaOld);
            MetaManager.Instance.AddMetaOperator(floatMetaNew.ID, floatMetaNew);
            MetaManager.Instance.AddMetaOperator(metacompositionOp.ID, metacompositionOp);

            var replaceCommand = new ReplaceOperatorCommand(metacompositionOp, floatMetaOld, floatMetaNew, internalOldID);
            replaceCommand.Do();

            var sourceID = metacompositionOp.Connections.ElementAt(0).SourceOpID;
            Assert.AreEqual(metacompositionOp.Connections.Count, 1);
            Assert.AreEqual(metacompositionOp.Operators[sourceID].Item1, floatMetaNew);

            MetaManager.Instance.RemoveMetaOperator(metacompositionOp.ID);
            MetaManager.Instance.RemoveMetaOperator(floatMetaNew.ID);
            MetaManager.Instance.RemoveMetaOperator(floatMetaOld.ID);
        }

        [TestMethod]
        public void Replace_OperatorWith1InWithOperatorWith1In_ConnectionHasSameSourceAndNewOpAsTarget()
        {
            var internalSourceID = Guid.NewGuid();
            var targetOpPartID = Guid.NewGuid();
            var sourceOpPartID = Guid.NewGuid();
            var connectionSourceOp = new MetaOperator(internalSourceID);

            var metaOutput = new MetaOutput(sourceOpPartID, "targetOutput", new MetaOperatorPart(Guid.NewGuid()));
            connectionSourceOp.AddOutput(metaOutput);

            var internalOldID = Guid.NewGuid();
            var floatMetaOld = OperatorTests.CreateMetaFloatWithoutConnections();
            var floatInput = new MetaInput(targetOpPartID, "targetInputOld", new MetaOperatorPart(Guid.NewGuid()), new Float(5.0f), false);
            floatMetaOld.AddInput(floatInput);
            
            var floatMetaNew = OperatorTests.CreateMetaFloatWithoutConnections();
            floatMetaNew.AddInput(new MetaInput(Guid.NewGuid(), "targetInputNew", new MetaOperatorPart(Guid.NewGuid()), new Float(5.0f), false));

            var metacompositionOp = new MetaOperator(Guid.NewGuid());
            metacompositionOp.AddOperator(connectionSourceOp);
            metacompositionOp.AddOperator(floatMetaOld, internalOldID);
            metacompositionOp.Connections = new[]
                                            {
                                                new MetaConnection(internalSourceID, sourceOpPartID, internalOldID, targetOpPartID)
                                            }.ToList();

            MetaManager.Instance.AddMetaOperator(floatMetaOld.ID, floatMetaOld);
            MetaManager.Instance.AddMetaOperator(floatMetaNew.ID, floatMetaNew);
            MetaManager.Instance.AddMetaOperator(metacompositionOp.ID, metacompositionOp);

            var replaceCommand = new ReplaceOperatorCommand(metacompositionOp, floatMetaOld, floatMetaNew, internalOldID);
            replaceCommand.Do();

            var targetID = metacompositionOp.Connections.ElementAt(0).TargetOpID;
            Assert.AreEqual(metacompositionOp.Connections.Count, 1);
            Assert.AreEqual(metacompositionOp.Operators[targetID].Item1, floatMetaNew);

            MetaManager.Instance.RemoveMetaOperator(metacompositionOp.ID);
            MetaManager.Instance.RemoveMetaOperator(floatMetaNew.ID);
            MetaManager.Instance.RemoveMetaOperator(floatMetaOld.ID);
        }

        [TestMethod]
        public void Replace_OperatorWith1InAnd1OutWithOperatorWith1InAnd1Out_ConnectionsReplacedCorrectly()
        {
            var internalSourceID = Guid.NewGuid();
            var sourceOpPartID = Guid.NewGuid();
            var connectionSourceOp = new MetaOperator(internalSourceID);

            var metaOutput = new MetaOutput(sourceOpPartID, "targetOutput", new MetaOperatorPart(Guid.NewGuid()));
            connectionSourceOp.AddOutput(metaOutput);

            var internalTargetID = Guid.NewGuid();
            var targetOpPartID = Guid.NewGuid();
            var connectionTargetOp = new MetaOperator(internalTargetID);

            var floatInput = new MetaInput(targetOpPartID, "targetInput", new MetaOperatorPart(Guid.NewGuid()), new Float(5.0f), false);
            connectionTargetOp.AddInput(floatInput);
            
            var internalOldID = Guid.NewGuid();
            var sourceOpPartIDOld = Guid.NewGuid();
            var targetOpPartIDOld = Guid.NewGuid();
            var floatMetaOld = OperatorTests.CreateMetaFloatWithoutConnections();
            var floatOutput = new MetaOutput(sourceOpPartIDOld, "sourceOutputOld", new MetaOperatorPart(Guid.NewGuid()));
            floatMetaOld.AddOutput(floatOutput);
            var floatInputOld = new MetaInput(targetOpPartIDOld, "targetInputOld", new MetaOperatorPart(Guid.NewGuid()), new Float(5.0f), false);
            floatMetaOld.AddInput(floatInputOld);

            var floatMetaNew = OperatorTests.CreateMetaFloatWithoutConnections();
            floatMetaNew.AddInput(new MetaInput(Guid.NewGuid(), "targetInputNew", new MetaOperatorPart(Guid.NewGuid()), new Float(5.0f), false));
            floatMetaNew.AddOutput(new MetaOutput(Guid.NewGuid(), "sourceOutputNew", new MetaOperatorPart(Guid.NewGuid())));

            var metacompositionOp = new MetaOperator(Guid.NewGuid());
            metacompositionOp.AddOperator(connectionTargetOp);
            metacompositionOp.AddOperator(connectionSourceOp);
            metacompositionOp.AddOperator(floatMetaOld, internalOldID);
            metacompositionOp.Connections = new[]
                                            {
                                                new MetaConnection(internalSourceID, sourceOpPartID, internalOldID, targetOpPartIDOld),
                                                new MetaConnection(internalOldID, sourceOpPartIDOld, internalTargetID, targetOpPartID)
                                            }.ToList();
            
            MetaManager.Instance.AddMetaOperator(floatMetaOld.ID, floatMetaOld);
            MetaManager.Instance.AddMetaOperator(floatMetaNew.ID, floatMetaNew);
            MetaManager.Instance.AddMetaOperator(metacompositionOp.ID, metacompositionOp);

            var replaceCommand = new ReplaceOperatorCommand(metacompositionOp, floatMetaOld, floatMetaNew, internalOldID);
            replaceCommand.Do();

            var targetID = metacompositionOp.Connections.Single(connection => connection.SourceOpID == internalSourceID).TargetOpID;
            var sourceID = metacompositionOp.Connections.Single(connection => connection.TargetOpID == internalTargetID).SourceOpID;
            Assert.AreEqual(metacompositionOp.Connections.Count, 2);
            Assert.AreEqual(metacompositionOp.Operators[targetID].Item1, floatMetaNew);
            Assert.AreEqual(metacompositionOp.Operators[sourceID].Item1, floatMetaNew);

            MetaManager.Instance.RemoveMetaOperator(metacompositionOp.ID);
            MetaManager.Instance.RemoveMetaOperator(floatMetaNew.ID);
            MetaManager.Instance.RemoveMetaOperator(floatMetaOld.ID);
        }

        [TestMethod]
        public void UndoReplace_OperatorWith1InAnd1OutWithOperatorWith1InAnd1Out_OldOperatorIsPartOfConnectionsAgain()
        {
            var internalSourceID = Guid.NewGuid();
            var sourceOpPartID = Guid.NewGuid();
            var connectionSourceOp = new MetaOperator(internalSourceID);

            var metaOutput = new MetaOutput(sourceOpPartID, "targetOutput", new MetaOperatorPart(Guid.NewGuid()));
            connectionSourceOp.AddOutput(metaOutput);

            var internalTargetID = Guid.NewGuid();
            var targetOpPartID = Guid.NewGuid();
            var connectionTargetOp = new MetaOperator(internalTargetID);

            var floatInput = new MetaInput(targetOpPartID, "targetInput", new MetaOperatorPart(Guid.NewGuid()), new Float(5.0f), false);
            connectionTargetOp.AddInput(floatInput);

            var internalOldID = Guid.NewGuid();
            var sourceOpPartIDOld = Guid.NewGuid();
            var targetOpPartIDOld = Guid.NewGuid();
            var floatMetaOld = OperatorTests.CreateMetaFloatWithoutConnections();
            var floatOutput = new MetaOutput(sourceOpPartIDOld, "sourceOutputOld", new MetaOperatorPart(Guid.NewGuid()));
            floatMetaOld.AddOutput(floatOutput);
            var floatInputOld = new MetaInput(targetOpPartIDOld, "targetInputOld", new MetaOperatorPart(Guid.NewGuid()), new Float(5.0f), false);
            floatMetaOld.AddInput(floatInputOld);

            var floatMetaNew = OperatorTests.CreateMetaFloatWithoutConnections();
            floatMetaNew.AddInput(new MetaInput(Guid.NewGuid(), "targetInputNew", new MetaOperatorPart(Guid.NewGuid()), new Float(5.0f), false));
            floatMetaNew.AddOutput(new MetaOutput(Guid.NewGuid(), "sourceOutputNew", new MetaOperatorPart(Guid.NewGuid())));

            var metacompositionOp = new MetaOperator(Guid.NewGuid());
            metacompositionOp.AddOperator(connectionTargetOp);
            metacompositionOp.AddOperator(connectionSourceOp);
            metacompositionOp.AddOperator(floatMetaOld, internalOldID);
            metacompositionOp.Connections = new[]
                                            {
                                                new MetaConnection(internalSourceID, sourceOpPartID, internalOldID, targetOpPartIDOld),
                                                new MetaConnection(internalOldID, sourceOpPartIDOld, internalTargetID, targetOpPartID)
                                            }.ToList();
            
            MetaManager.Instance.AddMetaOperator(floatMetaOld.ID, floatMetaOld);
            MetaManager.Instance.AddMetaOperator(floatMetaNew.ID, floatMetaNew);
            MetaManager.Instance.AddMetaOperator(metacompositionOp.ID, metacompositionOp);
            
            var replaceCommand = new ReplaceOperatorCommand(metacompositionOp, floatMetaOld, floatMetaNew, internalOldID);
            replaceCommand.Do();
            replaceCommand.Undo();

            var targetID = metacompositionOp.Connections.Single(connection => connection.SourceOpID == internalSourceID).TargetOpID;
            var sourceID = metacompositionOp.Connections.Single(connection => connection.TargetOpID == internalTargetID).SourceOpID;
            Assert.AreEqual(metacompositionOp.Connections.Count, 2);
            Assert.AreEqual(metacompositionOp.Operators[targetID].Item1, floatMetaOld);
            Assert.AreEqual(metacompositionOp.Operators[sourceID].Item1, floatMetaOld);

            MetaManager.Instance.RemoveMetaOperator(metacompositionOp.ID);
            MetaManager.Instance.RemoveMetaOperator(floatMetaNew.ID);
            MetaManager.Instance.RemoveMetaOperator(floatMetaOld.ID);
        }

        [TestMethod]
        public void Replace_OperatorWithMultiInputAnd3InComingConnections_SameOrderAtMultiInput()
        {   
            var multiInputID1 = Guid.NewGuid();
            var genericMultiInput1 = new MetaInput(multiInputID1, "multiInput", BasicMetaTypes.GenericMeta, new Generic(), true);
            var multiInputID2 = Guid.NewGuid();
            var genericMultiInput2 = new MetaInput(multiInputID2, "multiInput", BasicMetaTypes.GenericMeta, new Generic(), true);
            
            var genericMultiInputOperatorOld = new MetaOperator(Guid.NewGuid());
            genericMultiInputOperatorOld.AddInput(genericMultiInput1);

            var genericMultiInputOperatorNew = new MetaOperator(Guid.NewGuid());
            genericMultiInputOperatorNew.AddInput(genericMultiInput2);

            var sourceOutputID1 = Guid.NewGuid();
            var sourceOutput1 = new MetaOutput(sourceOutputID1, "sourceOutput1",  BasicMetaTypes.GenericMeta);
            var sourceOutputID2 = Guid.NewGuid();
            var sourceOutput2 = new MetaOutput(sourceOutputID2, "sourceOutput2", BasicMetaTypes.GenericMeta);

            var genericOutputOp1 = new MetaOperator(Guid.NewGuid());
            genericOutputOp1.AddOutput(sourceOutput1);
            var genericOutputOp2 = new MetaOperator(Guid.NewGuid());
            genericOutputOp2.AddOutput(sourceOutput2);

            var internalOldOperatorID = Guid.NewGuid();
            var internalSourceID1 = Guid.NewGuid();
            var internalSourceID2 = Guid.NewGuid();

            var metacompositionOp = new MetaOperator(Guid.NewGuid());
            metacompositionOp.AddOperator(genericMultiInputOperatorOld, internalOldOperatorID);
            metacompositionOp.AddOperator(genericOutputOp1, internalSourceID1);
            metacompositionOp.AddOperator(genericOutputOp2, internalSourceID2);
            metacompositionOp.Connections = new List<MetaConnection>
                                            {
                                                new MetaConnection(internalSourceID1, sourceOutputID1, internalOldOperatorID, multiInputID1),
                                                new MetaConnection(internalSourceID2, sourceOutputID2, internalOldOperatorID, multiInputID1),
                                                new MetaConnection(internalSourceID1, sourceOutputID1, internalOldOperatorID, multiInputID1)
                                            };

            MetaManager.Instance.AddMetaOperator(genericMultiInputOperatorOld.ID, genericMultiInputOperatorOld);
            MetaManager.Instance.AddMetaOperator(genericMultiInputOperatorNew.ID, genericMultiInputOperatorNew);
            MetaManager.Instance.AddMetaOperator(metacompositionOp.ID, metacompositionOp);

            var replaceCommand = new ReplaceOperatorCommand(metacompositionOp, genericMultiInputOperatorOld, genericMultiInputOperatorNew, internalOldOperatorID);
            replaceCommand.Do();

            var connectionsIntoNewMultiInput = metacompositionOp.Connections.Where(connection => connection.TargetOpPartID == multiInputID2).ToList();
            Assert.IsTrue(connectionsIntoNewMultiInput.Count() == 3);
            Assert.IsTrue(connectionsIntoNewMultiInput[0].SourceOpID == internalSourceID1);
            Assert.IsTrue(connectionsIntoNewMultiInput[1].SourceOpID == internalSourceID2);
            Assert.IsTrue(connectionsIntoNewMultiInput[2].SourceOpID == internalSourceID1);

            MetaManager.Instance.RemoveMetaOperator(metacompositionOp.ID);
            MetaManager.Instance.RemoveMetaOperator(genericMultiInputOperatorOld.ID);
            MetaManager.Instance.RemoveMetaOperator(genericMultiInputOperatorNew.ID);
        }
    }
}
