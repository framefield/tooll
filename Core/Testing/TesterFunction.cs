// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Framefield.Core.Testing
{
    public class TesterFunction : OperatorPart.Function
    {
        private enum InputId
        {
            Input = 0,
            Counter = 1,
            Count = 2,
            Threshold = 3
        }
        public virtual void SetupTestCase(OperatorPart subtree) { }
        public virtual bool GenerateData(OperatorPartContext context) { return true; }
        public virtual void StoreAsReferenceData(int index, int count) { }
        public virtual bool CompareData(int index, int count, float threshold, out string compareResultString) { compareResultString = String.Empty;  return false; }
        public virtual void CleanupData() { }

        public override OperatorPartContext Eval(OperatorPartContext context, List<OperatorPart> inputs, int outputIdx)
        {
            if (inputs.Count < 4)
            {
                context.Text = "";
                return context;
            }

            var Counter = inputs[(int)InputId.Counter].Eval(context).Text;
            var Count = (int)inputs[(int)InputId.Count].Eval(context).Value;
            var Threshold = inputs[(int)InputId.Threshold].Eval(context).Value;
            var Subtree = inputs[(int)InputId.Input];

            bool startTests;
            bool rebuildReference;
            string filter;
            Object evaluatorObj;
            if (context.Objects.TryGetValue(OperatorPartContext.TESTS_EVALUATOR_ID, out evaluatorObj))
            {
                var evaluator = evaluatorObj as ITestEvaluator;
                startTests = evaluator.GetStartTestsEnabled(context);
                rebuildReference = evaluator.GetRebuildReferenceEnabled(context);
                filter = evaluator.GetFilter(context);
            }
            else
            {
                Logger.Error("Test '{0}' could not be evaluated because no TestEvaluator was set previously.", GetFullTestCaseName(0, Count, '.'));
                context.Text = "";
                return context;
            }

            bool detectedStartTestsUpFlank = startTests && !_lastStartTests;
            bool detectedStartTestsDownFlank = !startTests && _lastStartTests;
            _lastStartTests = startTests;
            bool detectedRebuildReferenceUpFlank = rebuildReference && !_lastRebuildReference;
            bool detectedRebuildReferenceDownFlank = !rebuildReference && _lastRebuildReference;
            _lastRebuildReference = rebuildReference;

            if (detectedStartTestsUpFlank || detectedRebuildReferenceUpFlank)
            {
                context.Text = "";
                return context;
            }

            if (!detectedStartTestsUpFlank && !detectedStartTestsDownFlank && 
                !detectedRebuildReferenceUpFlank && !detectedRebuildReferenceDownFlank)
            {
                context.Text = "";
                return context;
            }


            String fullTestCaseName = GetFullTestCaseName(0, Count, '.');
            if (!Regex.IsMatch(fullTestCaseName, filter, RegexOptions.IgnoreCase))
            {
                context.Text = "";
                return context;
            }

            OperatorPart.ChangedPropagationEnabled = false;

            //collect all ops within the Subtree that access the Counter variable
            _collector.Clear();
            Subtree.TraverseWithFunction(_collector, null);
            _variableAccessorOpPartFunctions.Clear();

            foreach (var possibleOpPartFunction in _collector.CollectedOpPartFunctions)
            {
                if (possibleOpPartFunction.VariableName == Counter)
                    _variableAccessorOpPartFunctions.Add(possibleOpPartFunction as OperatorPart.Function);
            }

            String resultString = "";

            SetupTestCase(Subtree);

            for (int i = 0; i < Count; ++i)
            {
                if (context.Variables.ContainsKey(Counter))
                    context.Variables[Counter] = i;
                else
                    context.Variables.Add(Counter, i);

                foreach (var opPartFunc in _variableAccessorOpPartFunctions)
                    opPartFunc.OperatorPart.EmitChangedEvent();

                if (!GenerateData(context))
                    continue;

                if (detectedRebuildReferenceDownFlank)
                {
                    try
                    {
                        StoreAsReferenceData(i, Count);
                        resultString += String.Format("{0}: Test-reference updated", GetTestCaseName(i, Count));
                    }
                    catch (Exception ex)
                    {
                        resultString += String.Format(" {0}: Failed to update test-reference", GetTestCaseName(i, Count));
                        Logger.Error(ex.ToString());
                    }
                }
                else if (detectedStartTestsDownFlank)
                {
                    String matchingResult;
                    try
                    {
                        string compareResultString;
                        if (CompareData(i, Count, Threshold, out compareResultString))
                            matchingResult = "passed";
                        else
                            matchingResult = "FAILED";
                        matchingResult += String.Format(" ({0} < {1})", compareResultString, Threshold);
                    }
                    catch (Exception ex)
                    {
                        matchingResult = "Failed (unexpected reason)";
                        Logger.Error(ex.ToString());
                    }

                    resultString += String.Format("{0} : {1}", GetTestCaseName(i, Count), matchingResult);
                }

                CleanupData();

                resultString += '\n';
            }

            OperatorPart.ChangedPropagationEnabled = true;
            context.Text = resultString;
            return context;
        }

        public String GetReferenceFilename(int index, int count, string extension)
        {
            return MetaManager.OPERATOR_TEST_REFERENCE_PATH + GetFullTestCaseName(index, count, '/') + extension;
        }

        String GetFullTestCaseName(int index, int count, Char delimiter)
        {
            Operator testSuiteInWhichTheTestCaseIsEmbedded = OperatorPart.Parent.Parent;
            string opNamespace = testSuiteInWhichTheTestCaseIsEmbedded.Definition.Namespace;
            opNamespace = opNamespace.Replace("_test.", "");
            string path = opNamespace + 
                          testSuiteInWhichTheTestCaseIsEmbedded + '.' + 
                          GetTestCaseName(0, 0);
            path = path.Replace('.', delimiter);
            string filename = opNamespace + 
                              testSuiteInWhichTheTestCaseIsEmbedded + '.' + 
                              GetTestCaseName(index, count);
            return path + delimiter + filename;
        }

        String GetTestCaseName(int index, int count)
        {
            var name = OperatorPart.Parent.Name;
            if (name == string.Empty)
                name = OperatorPart.Parent.Definition.Name;

            if (count > 1)
                return String.Format("{0}.{1:000}", name, index);

            return String.Format("{0}", name);
        }

        bool _lastStartTests;
        bool _lastRebuildReference;
        readonly OperatorPart.CollectOpPartFunctionsOfType<OperatorPartTraits.IVariableAccessor> _collector = new OperatorPart.CollectOpPartFunctionsOfType<OperatorPartTraits.IVariableAccessor>();
        readonly List<OperatorPart.Function> _variableAccessorOpPartFunctions = new List<OperatorPart.Function>();
    }
}
