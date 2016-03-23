// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using Framefield.Core.Commands;

namespace Framefield.Core.Testing
{
    public static class TestUtilities
    {
        public static Tuple<bool, string> EvaluateTests(Guid testingMetaOpID, string filterPattern)
        {
            try
            {
                var result = new Tuple<bool, string>(false, "");

                var testingMetaOp = MetaManager.Instance.GetMetaOperator(testingMetaOpID);

                using (var mainOp = Utilities.CreateEmptyOperator())
                {
                    mainOp.Definition.Name = "Main";
                    MetaManager.Instance.AddMetaOperator(mainOp.Definition.ID, mainOp.Definition);

                    var addOpCmd = new AddOperatorCommand(mainOp, testingMetaOpID);
                    addOpCmd.Do();
                    var testingOp = testingMetaOp.GetOperatorInstance(addOpCmd.AddedInstanceID);

                    result = EvaluateTests(testingOp, filterPattern);

                    var deleteOperatorCmd = new DeleteOperatorsCommand(mainOp, new List<Operator>() { testingOp });
                    deleteOperatorCmd.Do();

                    MetaManager.Instance.RemoveMetaOperator(mainOp.Definition.ID);
                }
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to evaluate operator definition {0}: {1}", testingMetaOpID, ex.ToString());
                return new Tuple<bool, string>(false, "");
            }
        }

        public static Tuple<bool, string> EvaluateTests(Operator testingOp, string filterPattern)
        {
            try
            {
                //connect it to a testsevaluator
                var compositionOp = testingOp.Parent;

                var evaluatorMetaOpID = Guid.Parse("0316356c-b1fe-490a-89ce-73c8f67ebccc");
                var evaluatorMetaOp = MetaManager.Instance.GetMetaOperator(evaluatorMetaOpID);

                var addOpCmd = new AddOperatorCommand(compositionOp, evaluatorMetaOpID);
                addOpCmd.Do();
                var evaluatorOp = evaluatorMetaOp.GetOperatorInstance(addOpCmd.AddedInstanceID);

                var connection = new MetaConnection(testingOp.ID, testingOp.Outputs[0].ID,
                                                    evaluatorOp.ID, evaluatorOp.Inputs[0].ID);
                var addConnectionCmd = new InsertConnectionCommand(compositionOp.Definition, connection, 0);
                addConnectionCmd.Do();

                //configure the testsevaluator op and start testing
                var startTestsTriggerOpPart = evaluatorOp.Inputs[1];
                var filterOpPart = evaluatorOp.Inputs[3];

                //we must create a down flank for the startTestsTrigger value to start the tests properly
                var updateStartTestsCmd = new UpdateOperatorPartValueFunctionCommand(startTestsTriggerOpPart, new Float(1.0f));
                updateStartTestsCmd.Do();
                var updateFilterCmd = new UpdateOperatorPartValueFunctionCommand(filterOpPart, new Text(filterPattern));
                updateFilterCmd.Do();
                evaluatorOp.Outputs[0].Eval(new OperatorPartContext());

                updateStartTestsCmd.Value = new Float(0.0f);
                updateStartTestsCmd.Do();
                var resultLog = evaluatorOp.Outputs[0].Eval(new OperatorPartContext()).Text;

                var result = new Tuple<bool, string>(resultLog.StartsWith(compositionOp.Definition.Name + " : passed"), resultLog);

                var deleteOperatorCmd = new DeleteOperatorsCommand(compositionOp, new List<Operator>() { evaluatorOp });
                deleteOperatorCmd.Do();
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to evaluate operator definition {0}: {1}", testingOp.Name, ex.ToString());
                return new Tuple<bool, string>(false, "");
            }
        }
    }
}
