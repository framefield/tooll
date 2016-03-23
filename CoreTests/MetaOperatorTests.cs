// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.CodeDom.Compiler;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Framefield.Core;


namespace CoreTests
{

    using MetaOpEntry = Tuple<MetaOperator, MetaOperator.InstanceProperties>;
    using MetaOpEntryContainer = Dictionary<Guid, Tuple<MetaOperator, MetaOperator.InstanceProperties>>;

    [TestClass]
    public class MetaOperatorTests
    {
        #region HelperFunctions
        public static MetaOperator CreateFloatMetaOperator(Guid metaId) {
            var inputGuid = Guid.NewGuid();
            var outputGuid = Guid.NewGuid();

            return new MetaOperator(metaId) {
                Name = "Float",
                Inputs = new[] { new MetaInput(inputGuid, "Input", BasicMetaTypes.FloatMeta, new Float(0.0f), false) }.ToList(),
                Outputs = new[] { new MetaOutput(outputGuid, "Output", BasicMetaTypes.FloatMeta) }.ToList(),
                Operators = new MetaOpEntryContainer(),
                Connections = new[] { new MetaConnection(Guid.Empty, inputGuid, Guid.Empty, outputGuid) }.ToList(),
            };
        }

        public static MetaOperator CreateGenericMultiInputMetaOperator(Guid metaId = new Guid()) {
            var inputGuid = Guid.NewGuid();
            var outputGuid = Guid.NewGuid();

            return new MetaOperator(metaId) {
                Name = "Generic",
                Inputs = new[] { new MetaInput(inputGuid, "Input", BasicMetaTypes.GenericMeta, new Generic(), true) }.ToList(),
                Outputs = new[] { new MetaOutput(outputGuid, "Output", BasicMetaTypes.GenericMeta) }.ToList(),
                Operators = new MetaOpEntryContainer(),
                Connections = new[] { new MetaConnection(Guid.Empty, inputGuid, Guid.Empty, outputGuid) }.ToList(),
            };
        }

        public static MetaOperator CreateTextMetaOperator(Guid metaId = new Guid()) {
            var inputGuid = Guid.NewGuid();
            var outputGuid = Guid.NewGuid();

            return new MetaOperator(metaId) {
                Name = "Text",
                Inputs = new[] { new MetaInput(inputGuid, "Input", BasicMetaTypes.TextMeta, new Generic(), false) }.ToList(),
                Outputs = new[] { new MetaOutput(outputGuid, "Output", BasicMetaTypes.TextMeta) }.ToList(),
                Operators = new MetaOpEntryContainer(),
                Connections = new[] { new MetaConnection(Guid.Empty, inputGuid, Guid.Empty, outputGuid) }.ToList(),
            };
        }

        public static MetaOperator CreateSubMetaOp(float inputValue1, float inputValue2)
        {
            var input0 = new MetaInput(Guid.Parse("{AA522720-BB43-449F-9EAC-2FA2B13822C6}"), "Input0", BasicMetaTypes.FloatMeta, new Float(inputValue1), false);
            var input1 = new MetaInput(Guid.Parse("{9FC79D4C-3639-4A44-9ED8-69E8C5FF3E05}"), "Input1", BasicMetaTypes.FloatMeta, new Float(inputValue2), false);
            var output = new MetaOutput(Guid.Parse("{82B5272B-4884-4622-8891-2A9465550E18}"), "Output", BasicMetaTypes.FloatMeta);

            var opPartID = Guid.Parse("{3A6EA1C9-F479-4D89-A117-286D4DA49BCF}");
            var opPart = new MetaOperatorPart(Guid.Parse("{E6FA8A63-AAA5-4533-BC02-BD8E04157A2F}"))
                             {
                                 IsMultiInput = true,
                                 Name = "Func",
                                 Type = FunctionType.Float
                             };

            var metaOp = new MetaOperator(Guid.Parse("{5732DB8C-A1CC-48E7-85E3-3B3428957AF5}"))
                             {
                                 Name = "SimpleOp",
                                 Inputs = new[] { input0, input1 }.ToList(),
                                 Outputs = new[] { output }.ToList(),
                                 OperatorParts = new[] { new Tuple<Guid, MetaOperatorPart>(opPartID, opPart) }.ToList(),
                                 Connections = new[]
                                                   {
                                                       new MetaConnection(Guid.Empty, input0.ID, Guid.Empty, opPartID),
                                                       new MetaConnection(Guid.Empty, input1.ID, Guid.Empty, opPartID),
                                                       new MetaConnection(Guid.Empty, opPartID, Guid.Empty, output.ID)
                                                   }.ToList()
                             };

            var script = @"using System;
                           using System.Collections.Generic;
                           namespace Framefield.Core.IDca9a3a0e_c1c7_42b6_a0e5_cdb4c61d0b18
                           {
                               public class Class_Sub : OperatorPart.Function
                               {
                                   public override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs, int outputIdx) 
                                   {
                                        context.Value = input[0].Eval(context).Value - input[1].Eval(context).Value;
                                        return context;
                                   }
                               }
                           }";

            opPart.Script = script;
            opPart.Compile();

            return metaOp;
        }


//         public MetaOperator CreateMetaTest() {
//             var inputGuid = Guid.Parse("{13F05613-D1B2-4445-AFA5-3EA87C61C014}");
//             var outputGuid = Guid.Parse("{19D6314F-E892-402A-9F30-E6C47D725FD3}");
//             var randomGuid = Guid.Parse("{2F59B1AD-0E5B-4B62-A901-1384CBE5CF11}");
// 
//             var input = new MetaInput(inputGuid, "Input", BasicMetaTypes.FloatMeta, new Float(100.0f), false);
// 
//             var randomMetaOp = GlobalStuff.MetaManager.GetMetaOperator(GlobalStuff.RandomGUID);
// 
//             return new MetaOperator(Guid.Parse("{738C47E6-5D3C-46FE-9F14-A9FFCED6DEA8}")) {
//                                     Name = "Randomtest",
//                                     Inputs = new[] { input }.ToList(),
//                                     Outputs = new[] { new MetaOutput(outputGuid, "Output", BasicMetaTypes.FloatMeta) }.ToList(),
//                                     Operators = new MetaOpEntryContainer() {{ randomGuid, new MetaOpEntry(randomMetaOp, new MetaOperator.InstanceProperties(randomMetaOp.Inputs)) }},
//                                     Connections = new[] { new MetaConnection(Guid.Empty, inputGuid, randomGuid, randomMetaOp.Inputs[0].ID),
//                                                         new MetaConnection(randomGuid, randomMetaOp.Outputs[0].ID, Guid.Empty, outputGuid) }.ToList(),
//                    };
//         }
// 
// 
//         public MetaOperator CreateMetaFloatWithoutConnections() {
//             var metaOp = GlobalStuff.MetaManager.GetMetaOperator(GlobalStuff.ValueGUID).Clone();
//             metaOp.RemoveConnection(new MetaConnection(Guid.Empty, metaOp.Inputs[0].ID, Guid.Empty, metaOp.Outputs[0].ID), 0);
//             Assert.AreEqual(0, metaOp.Connections.Count);
//             return metaOp;
//         }
// 
//         public MetaOperator CreateMetaTest() {
//             var inputGuid = Guid.Parse("{13F05613-D1B2-4445-AFA5-3EA87C61C014}");
//             var outputGuid = Guid.Parse("{19D6314F-E892-402A-9F30-E6C47D725FD3}");
//             var randomGuid = Guid.Parse("{2F59B1AD-0E5B-4B62-A901-1384CBE5CF11}");
// 
//             var input = new MetaInput(inputGuid, "Input", BasicMetaTypes.FloatMeta, new Float(100.0f), false);
// 
//             var randomMetaOp = GlobalStuff.MetaManager.GetMetaOperator(GlobalStuff.RandomGUID);
// 
//             return new MetaOperator(Guid.Parse("{738C47E6-5D3C-46FE-9F14-A9FFCED6DEA8}")) {
//                                     Name = "Randomtest",
//                                     Inputs = new[] { input }.ToList(),
//                                     Outputs = new[] { new MetaOutput(outputGuid, "Output", BasicMetaTypes.FloatMeta) }.ToList(),
//                                     Operators = new MetaOpEntryContainer() {{ randomGuid, new MetaOpEntry(randomMetaOp, new MetaOperator.InstanceProperties(randomMetaOp.Inputs)) }},
//                                     Connections = new[] { new MetaConnection(Guid.Empty, inputGuid, randomGuid, randomMetaOp.Inputs[0].ID),
//                                                         new MetaConnection(randomGuid, randomMetaOp.Outputs[0].ID, Guid.Empty, outputGuid) }.ToList(),
//                    };
//         }
// 
// 
//         public MetaOperator CreateMetaOpWithoutConnections() {
//             var input1Guid = Guid.Parse("{3B807D92-5297-46DB-A6E2-DF9E77A95619}");
//             var input2Guid = Guid.Parse("{DAF2DBF9-E92E-49DF-8055-10662549AA71}");
//             var outputGuid = Guid.Parse("{FCCDFFF4-07C3-4C5C-9B4C-4562C7A17D66}");
//             var random1Guid = Guid.Parse("{A679E66E-307A-4548-9958-FE56B28F2578}");
//             var random2Guid = Guid.Parse("{483C7402-8336-4715-9A23-1F8B55110ADD}");
//             var multiplyGuid = Guid.Parse("{120FEC4C-9915-4550-AE59-CE6BD765B5C2}");
// 
//             var input1 = new MetaInput(input1Guid, "Input1", BasicMetaTypes.FloatMeta, new Float(100.0f), false);
//             var input2 = new MetaInput(input2Guid, "Input2", BasicMetaTypes.FloatMeta, new Float(100.0f), false);
// 
//             var randomMetaOp = GlobalStuff.MetaManager.GetMetaOperator(GlobalStuff.RandomGUID);
//             var multiplyMetaOp = GlobalStuff.MetaManager.GetMetaOperator(GlobalStuff.MultiplyGUID);
// 
//             return new MetaOperator(Guid.Parse("{D18802FE-4A1A-4866-838B-57856B1C3991}")) {
//                                     Name = "Randomtest2",
//                                     Inputs = new [] { input1, input2 }.ToList(),
//                                     Outputs = new [] { new MetaOutput(outputGuid, "Output", BasicMetaTypes.FloatMeta) }.ToList(),
//                                     Operators = new MetaOpEntryContainer() { 
//                                         { random1Guid, new MetaOpEntry(randomMetaOp, new MetaOperator.InstanceProperties(randomMetaOp.Inputs)) },
//                                         { random2Guid, new MetaOpEntry(randomMetaOp, new MetaOperator.InstanceProperties(randomMetaOp.Inputs)) },
//                                         { multiplyGuid, new MetaOpEntry(multiplyMetaOp, new MetaOperator.InstanceProperties(multiplyMetaOp.Inputs)) } 
//                                     }
//                    };
//         }
// 
// 
//         public MetaOperator CreateMeta2Test() {
//             var metaOp = CreateMetaOpWithoutConnections();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, metaOp.Inputs[0].ID, 
//                                                          metaOp.Operators[0].Key, metaOp.Operators[0].Item2.Inputs[0].ID), 0);
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, metaOp.Inputs[1].ID, 
//                                                          metaOp.Operators[1].Key, metaOp.Operators[1].Item2.Inputs[0].ID), 0);
//             metaOp.InsertConnectionAt(new MetaConnection(metaOp.Operators[0].Key, metaOp.Operators[0].Item2.Outputs[0].ID, 
//                                                          metaOp.Operators[2].Key, metaOp.Operators[2].Item2.Inputs[0].ID), 0);
//             metaOp.InsertConnectionAt(new MetaConnection(metaOp.Operators[1].Key, metaOp.Operators[1].Item2.Outputs[0].ID, 
//                                                          metaOp.Operators[2].Key, metaOp.Operators[2].Item2.Inputs[0].ID), 1);
//             metaOp.InsertConnectionAt(new MetaConnection(metaOp.Operators[2].Key, metaOp.Operators[2].Item2.Outputs[0].ID, 
//                                                          Guid.Empty, metaOp.Outputs[0].ID), 0);
//             return metaOp;
//         }
// 
// 
//         public MetaOperator CreateMeta3Test() {
//             var input1Guid = Guid.Parse("{5D180C7C-33E5-43AC-991F-64C91749D6B6}");
//             var input2Guid = Guid.Parse("{3207EF74-1E89-47C0-92AC-9676B890ABF5}");
//             var outputGuid = Guid.Parse("{00C2FCD4-AD0E-4076-9CD2-2355D64B8EC3}");
//             var randomGuid = Guid.Parse("{81595966-116A-4DDA-862F-7DFAAE0FA9CD}");
//             var testGuid = Guid.Parse("{2303895C-B96B-4DD5-9621-2446C41BA906}");
//             var multiplyGuid = Guid.Parse("{D51B3622-4EE2-4190-85F4-A5445488B62A}");
// 
//             var input1 = new MetaInput(input1Guid, "Input1", BasicMetaTypes.FloatMeta, new Float(100.0f), false);
//             var input2 = new MetaInput(input2Guid, "Input2", BasicMetaTypes.FloatMeta, new Float(100.0f), false);
// 
//             var randomMetaOp = GlobalStuff.MetaManager.GetMetaOperator(GlobalStuff.RandomGUID);
//             var testMetaOp = CreateMetaTest();
//             var multiplyMetaOp = GlobalStuff.MetaManager.GetMetaOperator(GlobalStuff.MultiplyGUID);
//         
//             return new MetaOperator(Guid.Parse("{B70035AE-D643-4BDD-95E6-7F7B3AD79E27}")) {
//                                     Name = "Randomtest3",
//                                     Inputs = new [] { input1, input2 }.ToList(),
//                                     Outputs = new [] { new MetaOutput(outputGuid, "Output", BasicMetaTypes.FloatMeta) }.ToList(),
//                                     Operators = new[] { Tuple.Create(testGuid, testMetaOp, new MetaOperator.InstanceProperties(testMetaOp.Inputs)), 
//                                                          Tuple.Create(randomGuid, randomMetaOp, new MetaOperator.InstanceProperties(randomMetaOp.Inputs)),
//                                                          Tuple.Create(multiplyGuid, multiplyMetaOp, new MetaOperator.InstanceProperties(multiplyMetaOp.Inputs)) }.ToList(),
//                                     Connections = new [] { new MetaConnection(Guid.Empty, input1Guid, testGuid, testMetaOp.Inputs[0].ID), 
//                                                            new MetaConnection(Guid.Empty, input2Guid, randomGuid, randomMetaOp.Inputs[0].ID),
//                                                            new MetaConnection(testGuid, testMetaOp.Outputs[0].ID, multiplyGuid, multiplyMetaOp.Inputs[0].ID),
//                                                            new MetaConnection(randomGuid, randomMetaOp.Outputs[0].ID, multiplyGuid, multiplyMetaOp.Inputs[0].ID),
//                                                            new MetaConnection(multiplyGuid, multiplyMetaOp.Outputs[0].ID, Guid.Empty, outputGuid) }.ToList()
//                    };
//         }
        #endregion

//         #region Creation tests
//         [TestMethod]
//         public void testCreateMetaMacroOperator() {
//             var meta = CreateMetaTest();
//             var op = meta.CreateOperator(Guid.NewGuid());
// 
//             var context = new OperatorPartContext();
//             op.Inputs[0].Func = Utilities.CreateValueFunction(new Float(10.0f));
//             Assert.IsTrue(Utilities.IsEqual(0.9504964f, op.Outputs[0].Eval(context).Value));
//         }
// 
//         [TestMethod]
//         public void testCreateMetaMacro2Operator() {
//             var meta = CreateMeta2Test();
//             var op = meta.CreateOperator(Guid.NewGuid());
// 
//             var testContext = new OperatorPartContext();
//             op.Inputs[0].Func = Utilities.CreateValueFunction(new Float(10.0f));
//             op.Inputs[1].Func = Utilities.CreateValueFunction(new Float(10.0f));
//             Assert.IsTrue(Utilities.IsEqual(0.9034435f, op.Outputs[0].Eval(testContext).Value));
//         }
// 
//         [TestMethod]
//         public void testCreateMetaMacro3Operator() {
//             var meta = CreateMeta3Test();
//             var op = meta.CreateOperator(Guid.NewGuid());
// 
//             var testContext = new OperatorPartContext();
//             op.Inputs[0].Func = Utilities.CreateValueFunction(new Float(10.0f));
//             op.Inputs[1].Func = Utilities.CreateValueFunction(new Float(10.0f));
//             Assert.IsTrue(Utilities.IsEqual(0.9034435f, op.Outputs[0].Eval(testContext).Value));
//         }
// 
//         [TestMethod]
//         public void CreateOperator_Create2Ops_BothInstancesOfFunctionCodeAreNotEqual() {
//             var metaOp = createSimpleMetaOp();
//             var metaOpPart = metaOp.OperatorParts[0].Item2;
// 
//             var script = 
//                 @"using System;
//                   using System.Collections.Generic;
//                   namespace Framefield.Core
//                   {
//                       public class Class_bla : OperatorPart.Function
//                       {
//                           public override event EventHandler<System.EventArgs> ChangedEvent = (o, a) => {};
//                           public override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs) {
//                               context.Value = this.GetHashCode();
//                               return context;
//                           }
//                       }
//                   }";
// 
//             metaOpPart.Version = Guid.NewGuid();
//             metaOpPart.Script = script;
// 
//             var testContext = new OperatorPartContext();
//             var op1 = metaOp.CreateOperator(Guid.NewGuid());
//             var op2 = metaOp.CreateOperator(Guid.NewGuid());
//             Assert.AreNotEqual(op1.Outputs[0].Eval(testContext).Value, op2.Outputs[0].Eval(testContext).Value);
//         }
//         #endregion
// 
//         #region Clone
//         [TestMethod]
//         public void Clone_EmptyMetaOp_returnEmptyMetaOpWithDifferentID() {
//             var emptyMetaOp = new MetaOperator(Guid.NewGuid()) { Name = "Empty" };
//             var copiedEmptyMetaOp = emptyMetaOp.Clone();
//             Assert.AreEqual(emptyMetaOp.Name, copiedEmptyMetaOp.Name);
//             Assert.AreNotEqual(emptyMetaOp.ID, copiedEmptyMetaOp.ID);
//         }
// 
//         [TestMethod]
//         public void Clone_ModifyClonedMetaOp_originalMetaOpRemainsUnchanged() {
//             var metaOp = createSimpleMetaOp();
//             var op = metaOp.CreateOperator(Guid.NewGuid());
// 
//             var copiedMetaOp = metaOp.Clone();
//             var copiedMetaOpPart = copiedMetaOp.OperatorParts[0].Item2;
//             var copiedOp = copiedMetaOp.CreateOperator(Guid.NewGuid());
// 
//             var script = 
//                 @"
//                   using System;
//                   using System.Collections.Generic;
//                   namespace Framefield.Core
//                   {
//                       public class Class_bla : OperatorPart.Function
//                       {
//                           public override event EventHandler<System.EventArgs> ChangedEvent = (o, a) => {};
//                           public override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs) {
//                               var input0 = (float)inputs[0].Eval(context).Value;
//                               var input1 = (float)inputs[1].Eval(context).Value;
//                               context.Text = input0.ToString() + input1.ToString();
//                               return context;
//                           }
//                       }
//                   }";
// 
//             copiedMetaOp.RemoveAllOutputs();
//             copiedMetaOp.AddOutput(new MetaOutput(Guid.NewGuid(), "new output", BasicMetaTypes.TextMeta));
//             copiedOp.AppendConnection(new Connection(copiedOp, copiedOp.InternalParts[0], copiedOp, copiedOp.Outputs[0], 0));
// 
//             copiedMetaOpPart.Script = script;
// 
//             Assert.AreEqual(3.0f, op.Outputs[0].Eval(new OperatorPartContext()).Value);
//             Assert.AreEqual("107", copiedOp.Outputs[0].Eval(new OperatorPartContext()).Text);
//         }
//         #endregion
// 
//         #region Operator tests
//         [TestMethod]
//         public void AddOperator_EmptyOperator_resultContains1Operator() {
//             var op = Utilities.CreateEmptyOperator();
//             Assert.AreEqual(0, op.InternalOps.Count);
//                      
//             op.Definition.AddOperator(GlobalStuff.MetaManager.GetMetaOperator(GlobalStuff.ValueGUID));
//     
//             var newOperator = op.Definition.CreateOperator(Guid.NewGuid());
//             Assert.AreEqual(1, newOperator.InternalOps.Count);
//         }
// 
//         [TestMethod]
//         public void RemoveOperator_OperatorWithInternalOp_resultContainsNoOperator() {
//             var op = Utilities.CreateEmptyOperator();
//             op.Definition.AddOperator(GlobalStuff.MetaManager.GetMetaOperator(GlobalStuff.ValueGUID));
//             var operatorWithInternalOp = op.Definition.CreateOperator(Guid.NewGuid());
//             Assert.AreEqual(1, operatorWithInternalOp.InternalOps.Count);
// 
//             op.Definition.RemoveOperator(operatorWithInternalOp.InternalOps[0].ID);
//             Assert.AreEqual(0, operatorWithInternalOp.InternalOps.Count);
//             Assert.AreEqual(0, operatorWithInternalOp.Definition.Operators.Count);
//         }
//         #endregion
// 
//         #region Connection tests
//         [TestMethod]
//         public void InsertConnectionAt_Index0IntoEmptyConnections_connectionInserted() {
//             var metaOp = CreateMetaFloatWithoutConnections();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, metaOp.Inputs[0].ID, Guid.Empty, metaOp.Outputs[0].ID), 0);
//             Assert.AreEqual(1, metaOp.Connections.Count);
//         }
// 
//         [TestMethod]
//         public void InsertConnectionAt_Index1IntoExistingConnection_connectionInsertedAtEnd() {
//             var metaOp = CreateMetaFloatWithoutConnections();
//             var opPartID1 = Guid.NewGuid();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, opPartID1, Guid.Empty, metaOp.Outputs[0].ID), 0);
// 
//             var opPartID2 = Guid.NewGuid();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, opPartID2, Guid.Empty, metaOp.Outputs[0].ID), 1);
//             Assert.AreEqual(2, metaOp.Connections.Count);
//             Assert.AreEqual(opPartID1, metaOp.Connections[0].SourceOpPartID);
//             Assert.AreEqual(opPartID2, metaOp.Connections[1].SourceOpPartID);
//         }
// 
//         [TestMethod]
//         public void InsertConnectionAt_Index0IntoExistingConnection_connectionInsertedAtBegin() {
//             var metaOp = CreateMetaFloatWithoutConnections();
//             var opPartID1 = Guid.NewGuid();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, opPartID1, Guid.Empty, metaOp.Outputs[0].ID), 0);
// 
//             var opPartID2 = Guid.NewGuid();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, opPartID2, Guid.Empty, metaOp.Outputs[0].ID), 0);
//             Assert.AreEqual(2, metaOp.Connections.Count);
//             Assert.AreEqual(opPartID2, metaOp.Connections[0].SourceOpPartID);
//             Assert.AreEqual(opPartID1, metaOp.Connections[1].SourceOpPartID);
//         }
// 
//         [TestMethod]
//         public void InsertConnectionAt_Index0IntoExistingConnectionsToDifferentTarget_connectionInsertedAtEnd() {
//             var metaOp = CreateMetaFloatWithoutConnections();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, metaOp.Inputs[0].ID, Guid.Empty, metaOp.Outputs[0].ID), 0);
// 
//             var targetOpPartID = Guid.NewGuid();
//             var opPartID1 = Guid.NewGuid();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, opPartID1, Guid.Empty, targetOpPartID), 0);
//             Assert.AreEqual(2, metaOp.Connections.Count);
//             Assert.AreEqual(opPartID1, metaOp.Connections[1].SourceOpPartID);
//         }
// 
//         [TestMethod]
//         public void InsertConnectionAt_Index0IntoExistingConnectionsToDifferentAndTheSameTarget_connectionInsertedAtBeginOfTheSameTargetConnections() {
//             var metaOp = CreateMetaFloatWithoutConnections();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, metaOp.Inputs[0].ID, Guid.Empty, metaOp.Outputs[0].ID), 0);
// 
//             var targetOpPartID = Guid.NewGuid();
//             var opPartID1 = Guid.NewGuid();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, opPartID1, Guid.Empty, targetOpPartID), 0);
// 
//             var opPartID2 = Guid.NewGuid();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, opPartID2, Guid.Empty, targetOpPartID), 0);
//             Assert.AreEqual(3, metaOp.Connections.Count);
//             Assert.AreEqual(opPartID2, metaOp.Connections[1].SourceOpPartID);
//             Assert.AreEqual(opPartID1, metaOp.Connections[2].SourceOpPartID);
//         }
// 
//         [TestMethod]
//         public void InsertConnectionAt_Index1IntoExistingConnectionsToDifferentAndTheSameTarget_connectionInsertedAtEndOfTheSameTargetConnections() {
//             var metaOp = CreateMetaFloatWithoutConnections();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, metaOp.Inputs[0].ID, Guid.Empty, metaOp.Outputs[0].ID), 0);
// 
//             var targetOpPartID = Guid.NewGuid();
//             var opPartID1 = Guid.NewGuid();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, opPartID1, Guid.Empty, targetOpPartID), 0);
// 
//             var opPartID2 = Guid.NewGuid();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, opPartID2, Guid.Empty, targetOpPartID), 1);
//             Assert.AreEqual(3, metaOp.Connections.Count);
//             Assert.AreEqual(opPartID1, metaOp.Connections[1].SourceOpPartID);
//             Assert.AreEqual(opPartID2, metaOp.Connections[2].SourceOpPartID);
//         }
// 
//         [TestMethod]
//         [ExpectedException(typeof(System.Exception))]
//         public void RemoveConnection_ConnectionDoesNotExists_throws() {
//             var metaOp = CreateMetaFloatWithoutConnections();
//             metaOp.RemoveConnection(new MetaConnection(Guid.Empty, metaOp.Inputs[0].ID, Guid.Empty, metaOp.Outputs[0].ID), 0);
//         }
// 
//         [TestMethod]
//         public void RemoveConnectionAt_Index0FromExistingConnectionTo1Target_connectionRemovedAtBegin() {
//             var metaOp = CreateMetaFloatWithoutConnections();
//             var opPartID1 = Guid.NewGuid();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, opPartID1, Guid.Empty, metaOp.Outputs[0].ID), 0);
//             var opPartID2 = Guid.NewGuid();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, opPartID2, Guid.Empty, metaOp.Outputs[0].ID), 1);
// 
//             metaOp.RemoveConnection(new MetaConnection(Guid.Empty, opPartID1, Guid.Empty, metaOp.Outputs[0].ID), 0);
//             Assert.AreEqual(1, metaOp.Connections.Count);
//             Assert.AreEqual(opPartID2, metaOp.Connections[0].SourceOpPartID);
//         }
// 
//         [TestMethod]
//         public void RemoveConnectionAt_Index1FromExistingConnectionTo1Target_connectionRemovedAtEnd() {
//             var metaOp = CreateMetaFloatWithoutConnections();
//             var opPartID1 = Guid.NewGuid();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, opPartID1, Guid.Empty, metaOp.Outputs[0].ID), 0);
//             var opPartID2 = Guid.NewGuid();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, opPartID2, Guid.Empty, metaOp.Outputs[0].ID), 1);
// 
//             metaOp.RemoveConnection(new MetaConnection(Guid.Empty, opPartID2, Guid.Empty, metaOp.Outputs[0].ID), 1);
//             Assert.AreEqual(1, metaOp.Connections.Count);
//             Assert.AreEqual(opPartID1, metaOp.Connections[0].SourceOpPartID);
//         }
// 
//         [TestMethod]
//         public void RemoveConnectionAt_Index0FromExistingConnectionToDifferentAndTheSameTarget_connectionRemovedAtBeginOfTheSameTargetConnections() {
//             var metaOp = CreateMetaFloatWithoutConnections();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, metaOp.Inputs[0].ID, Guid.Empty, metaOp.Outputs[0].ID), 0);
//             var targetOpPartID = Guid.NewGuid();
//             var opPartID1 = Guid.NewGuid();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, opPartID1, Guid.Empty, targetOpPartID), 0);
//             var opPartID2 = Guid.NewGuid();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, opPartID2, Guid.Empty, targetOpPartID), 1);
// 
//             metaOp.RemoveConnection(new MetaConnection(Guid.Empty, opPartID1, Guid.Empty, targetOpPartID), 0);
//             Assert.AreEqual(2, metaOp.Connections.Count);
//             Assert.AreEqual(opPartID2, metaOp.Connections[1].SourceOpPartID);
//         }
// 
//         [TestMethod]
//         public void RemoveConnectionAt_Index1FromExistingConnectionToDifferentAndTheSameTarget_connectionRemovedAtEndOfTheSameTargetConnections() {
//             var metaOp = CreateMetaFloatWithoutConnections();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, metaOp.Inputs[0].ID, Guid.Empty, metaOp.Outputs[0].ID), 0);
//             var targetOpPartID = Guid.NewGuid();
//             var opPartID1 = Guid.NewGuid();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, opPartID1, Guid.Empty, targetOpPartID), 0);
//             var opPartID2 = Guid.NewGuid();
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, opPartID2, Guid.Empty, targetOpPartID), 1);
// 
//             metaOp.RemoveConnection(new MetaConnection(Guid.Empty, opPartID2, Guid.Empty, targetOpPartID), 1);
//             Assert.AreEqual(2, metaOp.Connections.Count);
//             Assert.AreEqual(opPartID1, metaOp.Connections[1].SourceOpPartID);
//         }
// 
//         [TestMethod]
//         public void AddConnection_MetaOpWithoutConnections_buildsConnectionsWithinOp() {
//             var metaOp = CreateMetaOpWithoutConnections();
//             var op = metaOp.CreateOperator(Guid.NewGuid());
//             Assert.AreEqual(0, metaOp.Connections.Count);
// 
//             op.AppendConnection(new Connection(op, op.Inputs[0], op.InternalOps[0], op.InternalOps[0].Inputs[0], 0));
//             op.AppendConnection(new Connection(op, op.Inputs[1], op.InternalOps[1], op.InternalOps[1].Inputs[0], 0));
//             op.AppendConnection(new Connection(op.InternalOps[0], op.InternalOps[0].Outputs[0], op.InternalOps[2], op.InternalOps[2].Inputs[0], 0));
//             op.AppendConnection(new Connection(op.InternalOps[1], op.InternalOps[1].Outputs[0], op.InternalOps[2], op.InternalOps[2].Inputs[0], 0));
//             op.AppendConnection(new Connection(op.InternalOps[2], op.InternalOps[2].Outputs[0], op, op.Outputs[0], 0));
// 
//             var testContext = new OperatorPartContext();
//             op.Inputs[0].Func = Utilities.CreateValueFunction(new Float(10.0f));
//             op.Inputs[1].Func = Utilities.CreateValueFunction(new Float(10.0f));
//             Assert.IsTrue(Utilities.IsEqual(0.9034435f, op.Outputs[0].Eval(testContext).Value));
//         }
// 
//         [TestMethod]
//         public void AddConnection_MetaOpWithoutConnections_buildsConnectionsWithinCorrespondingMetaOp() {
//             var metaOp = CreateMetaOpWithoutConnections();
//             var op = metaOp.CreateOperator(Guid.NewGuid());
//             Assert.AreEqual(0, metaOp.Connections.Count);
// 
//             op.AppendConnection(new Connection(op, op.Inputs[0], op.InternalOps[0], op.InternalOps[0].Inputs[0], 0));
//             op.AppendConnection(new Connection(op, op.Inputs[1], op.InternalOps[1], op.InternalOps[1].Inputs[0], 0));
//             op.AppendConnection(new Connection(op.InternalOps[0], op.InternalOps[0].Outputs[0], op.InternalOps[2], op.InternalOps[2].Inputs[0], 0));
//             op.AppendConnection(new Connection(op.InternalOps[1], op.InternalOps[1].Outputs[0], op.InternalOps[2], op.InternalOps[2].Inputs[0], 0));
//             op.AppendConnection(new Connection(op.InternalOps[2], op.InternalOps[2].Outputs[0], op, op.Outputs[0], 0));
// 
//             //at this point the meta op must also contain the connections
//             Assert.AreEqual(5, metaOp.Connections.Count);
//             var opWithConnections = metaOp.CreateOperator(Guid.NewGuid());
// 
//             var testContext = new OperatorPartContext();
//             op.Inputs[0].Func = Utilities.CreateValueFunction(new Float(10.0f));
//             op.Inputs[1].Func = Utilities.CreateValueFunction(new Float(10.0f));
//             Assert.IsTrue(Utilities.IsEqual(0.9034435f, op.Outputs[0].Eval(testContext).Value));
//         }
// 
//         [TestMethod]
//         public void RemoveConnection_ValidMetaOp_removesConnectionsWithinOp() {
//             var metaOp = CreateMeta2Test();
//             Assert.AreEqual(5, metaOp.Connections.Count);
//             var op = metaOp.CreateOperator(Guid.NewGuid());
// 
//             //remove the connection to the output
//             op.RemoveConnection(new Connection(op.InternalOps[2], op.InternalOps[2].Outputs[0], op, op.Outputs[0], 0));
//             Assert.AreEqual(4, metaOp.Connections.Count);
//         }
// 
//         [TestMethod]
//         public void RemoveConnection_ValidMetaOp_removesConnectionsWithinCorrespondingMetaOp() {
//             var metaOp = CreateMeta2Test();
//             Assert.AreEqual(5, metaOp.Connections.Count);
//             var op = metaOp.CreateOperator(Guid.NewGuid());
// 
//             //remove the connection to the output
//             op.RemoveConnection(new Connection(op.InternalOps[2], op.InternalOps[2].Outputs[0], op, op.Outputs[0], 0));
//             Assert.AreEqual(4, metaOp.Connections.Count);
//         }
//         #endregion

//         #region Input/Output manipulation tests
//         public static MetaOperator createSimpleMetaOp() {
//             var input0 = new MetaInput(Guid.Parse("{AA522720-BB43-449F-9EAC-2FA2B13822C6}"), "Input0", BasicMetaTypes.FloatMeta, new Float(10.0f), false);
//             var input1 = new MetaInput(Guid.Parse("{9FC79D4C-3639-4A44-9ED8-69E8C5FF3E05}"), "Input1", BasicMetaTypes.FloatMeta, new Float(7.0f), false);
//             var output = new MetaOutput(Guid.Parse("{82B5272B-4884-4622-8891-2A9465550E18}"), "Output", BasicMetaTypes.FloatMeta);
// 
//             var opPartID = Guid.Parse("{3A6EA1C9-F479-4D89-A117-286D4DA49BCF}");
//             var opPart = new MetaOperatorPart(Guid.Parse("{E6FA8A63-AAA5-4533-BC02-BD8E04157A2F}")) {
//                                               IsMultiInput = true,
//                                               Name = "Func",
//                                               Type = FunctionType.Float};
// 
//             var metaOp = new MetaOperator(Guid.Parse("{5732DB8C-A1CC-48E7-85E3-3B3428957AF5}")) {
//                                           Name = "SimpleOp",
//                                           Inputs = new[] { input0, input1 }.ToList(),
//                                           Outputs = new[] { output }.ToList(),
//                                           OperatorParts = new[] { new Tuple<Guid, MetaOperatorPart>(opPartID, opPart) }.ToList(),
//                                           Connections = new[] { new MetaConnection(Guid.Empty, input0.ID, Guid.Empty, opPartID),
//                                                                 new MetaConnection(Guid.Empty, input1.ID, Guid.Empty, opPartID),
//                                                                 new MetaConnection(Guid.Empty, opPartID, Guid.Empty, output.ID) }.ToList()
//                                           };
// 
//             string code = "context.Value = Input0 - Input1;\r" +
//                           "return context;\r";
// 
// 
//             opPart.Script = CodeGenerator.Generate(metaOp, opPart, code);
// 
//             return metaOp;
//         }
// 
//         [TestMethod]
//         public void InputManipulation_AddInput_resultIsWorking() {
//             var metaOp = createSimpleMetaOp();
//             var metaOpPart = metaOp.OperatorParts[0].Item2;
//             var op = metaOp.CreateOperator(Guid.NewGuid());
// 
//             var script = 
//                 @"using System;
//                   using System.Collections.Generic;
//                   namespace Framefield.Core
//                   {
//                       public class Class_bla : OperatorPart.Function
//                       {
//                           public override event EventHandler<System.EventArgs> ChangedEvent = (o, a) => {};
//                           public override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs) {
//                               context.Value = inputs[2].Eval(context).Value;
//                               return context;
//                           }
//                       }
//                   }";
// 
//             metaOp.AddInput(new MetaInput(Guid.NewGuid(), "AddedInput", BasicMetaTypes.FloatMeta, new Float(42.0f), false));
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, op.Inputs[2].ID, Guid.Empty, op.InternalParts[0].ID), 2);
// 
//             metaOpPart.Version = Guid.NewGuid();
//             metaOpPart.Script = script;
// 
//             var testContext = new OperatorPartContext();
//             Assert.AreEqual(42.0f, op.Outputs[0].Eval(testContext).Value);
//         }
// 
//         [TestMethod]
//         public void InputManipulation_RemoveInput_resultIsWorking() {
//             var metaOp = createSimpleMetaOp();
//             var metaOpPart = metaOp.OperatorParts[0].Item2;
//             var op = metaOp.CreateOperator(Guid.NewGuid());
// 
//             var script = 
//                 @"
//                   using System;
//                   using System.Collections.Generic;
//                   namespace Framefield.Core
//                   {
//                       public class Class_bla : OperatorPart.Function
//                       {
//                           public override event EventHandler<System.EventArgs> ChangedEvent = (o, a) => {};
//                           public override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs) {
//                               context.Value = inputs[0].Eval(context).Value;
//                               return context;
//                           }
//                       }
//                   }";
// 
//             var input = metaOp.Inputs[0];
//             metaOp.RemoveInput(input.ID);
// 
//             metaOpPart.Version = Guid.NewGuid();
//             metaOpPart.Script = script;
// 
//             var testContext = new OperatorPartContext();
//             Assert.AreEqual(7.0f, op.Outputs[0].Eval(testContext).Value);
//         }
// 
//         [TestMethod]
//         public void InputManipulation_RemoveInput_ConnectionFromInputIsRemoved() {
//             var metaOp = createSimpleMetaOp();
// 
//             metaOp.RemoveInput(metaOp.Inputs[0].ID);
// 
//             Assert.AreEqual(2, metaOp.Connections.Count);
//         }
// 
//         [TestMethod]
//         public void InputManipulation_RemoveAllInputs_allInputsAreRemoved() {
//             var metaOp = createSimpleMetaOp();
//             metaOp.RemoveAllInputs();
// 
//             Assert.AreEqual(0, metaOp.Inputs.Count);
//         }
// 
//         [TestMethod]
//         public void InputManipulation_ChangeInputType_resultIsWorking() {
//             var metaOp = createSimpleMetaOp();
//             var metaOpPart = metaOp.OperatorParts[0].Item2;
//             var op = metaOp.CreateOperator(Guid.NewGuid());
// 
//             var script = 
//                 @"
//                   using System;
//                   using System.Collections.Generic;
//                   namespace Framefield.Core
//                   {
//                       public class Class_bla : OperatorPart.Function
//                       {
//                           public override event EventHandler<System.EventArgs> ChangedEvent = (o, a) => {};
//                           public override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs) {
//                               var input0 = inputs[0].Eval(context).Value;
//                               var input1 = inputs[1].Eval(context).Text;
//                               context.Value = input0 - (float)input1.Length;
//                               return context;
//                           }
//                       }
//                   }";
// 
//             metaOp.AddInput(new MetaInput(Guid.NewGuid(), metaOp.Inputs[1].Name, BasicMetaTypes.TextMeta, new Text("blaa"), false));
//             metaOp.RemoveInput(metaOp.Inputs[1].ID);
//             metaOp.InsertConnectionAt(new MetaConnection(Guid.Empty, op.Inputs[1].ID, Guid.Empty, op.InternalParts[0].ID), 1);
// 
//             metaOpPart.Version = Guid.NewGuid();
//             metaOpPart.Script = script;
// 
//             var testContext = new OperatorPartContext();
//             Assert.AreEqual(6.0f, op.Outputs[0].Eval(testContext).Value);
//         }
// 
//         [TestMethod]
//         public void OutputManipulation_RemoveOutput_ConnectionFromInputIsRemoved() {
//             var metaOp = createSimpleMetaOp();
// 
//             metaOp.RemoveOutput(metaOp.Outputs[0].ID);
// 
//             Assert.AreEqual(2, metaOp.Connections.Count);
//         }
// 
//         [TestMethod]
//         public void OutputManipulation_RemoveAllOutputs_allOutputsAreRemoved() {
//             var metaOp = createSimpleMetaOp();
//             metaOp.RemoveAllOutputs();
// 
//             Assert.AreEqual(0, metaOp.Outputs.Count);
//         }
// 
//         [TestMethod]
//         public void OutputManipulation_ChangeOutputType_resultIsWorking() {
//             var metaOp = createSimpleMetaOp();
//             var metaOpPart = metaOp.OperatorParts[0].Item2;
//             var op = metaOp.CreateOperator(Guid.NewGuid());
// 
//             var script = 
//                 @"
//                   using System;
//                   using System.Collections.Generic;
//                   namespace Framefield.Core
//                   {
//                       public class Class_bla : OperatorPart.Function
//                       {
//                           public override event EventHandler<System.EventArgs> ChangedEvent = (o, a) => {};
//                           public override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs) {
//                               var input0 = (float)inputs[0].Eval(context).Value;
//                               var input1 = (float)inputs[1].Eval(context).Value;
//                               context.Text = input0.ToString() + input1.ToString();
//                               return context;
//                           }
//                       }
//                   }";
// 
// 
//             metaOp.RemoveAllOutputs();
//             metaOp.AddOutput(new MetaOutput(Guid.NewGuid(), "new output", BasicMetaTypes.TextMeta));
//             op.AppendConnection(new Connection(op, op.InternalParts[0], op, op.Outputs[0], 0));
// 
//             metaOpPart.Version = Guid.NewGuid();
//             metaOpPart.Script = script;
// 
//             var testContext = new OperatorPartContext();
//             Assert.AreEqual("107", op.Outputs[0].Eval(testContext).Text);
//         }
//         #endregion

    }
}
