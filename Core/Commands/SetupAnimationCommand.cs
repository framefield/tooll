// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Framefield.Core.Commands
{
    public class SetupAnimationCommand : MacroCommand
    {
        private static Guid CurveID = new Guid("145c3a6b-b91f-450d-ac46-b13c66ebce19");
        private static Guid CurrentTimeID = new Guid("253e302b-8141-4d17-96ee-42af092dbf59");

        public SetupAnimationCommand() { }

        public SetupAnimationCommand(IEnumerable<OperatorPart> opParts, double keyframeTime)
        {
            _name = "Setup Animation";
            _commands = new List<ICommand>();
            foreach (var opPart in opParts)
            {
                _commands.AddRange(SetupSingleAnimation(opPart, keyframeTime));
            }
        }

        public SetupAnimationCommand(OperatorPart opPart, double keyframeTime)
            : this(new[] { opPart }, keyframeTime)
        {
        }

        private List<ICommand> SetupSingleAnimation(OperatorPart opPart, double keyframeTime)
        {
            var context = new OperatorPartContext() { Time = (float)keyframeTime };
            float currentValue = opPart.Eval(context).Value;

            // this command is needed to restore the original value correctly when undoing this, for doing it it's redundant
            var setValueCommand = new SetFloatValueCommand(opPart, currentValue);

            var compOp = opPart.Parent.Parent;
            var addCurveOpCommand = new AddOperatorCommand(compOp, CurveID, 100, 100, 100, false);
            var curveOpInstanceId = addCurveOpCommand.AddedInstanceID;
            var addTimeOpCommand = new AddOperatorCommand(compOp, CurrentTimeID, 100, 100, 100, false);

            var curveMetaOp = MetaManager.Instance.GetMetaOperator(CurveID);
            var timeMetaOp = MetaManager.Instance.GetMetaOperator(CurrentTimeID);

            var timeToCurve = new MetaConnection(addTimeOpCommand.AddedInstanceID, timeMetaOp.Outputs[0].ID,
                                                 addCurveOpCommand.AddedInstanceID, curveMetaOp.Inputs[0].ID);
            var connectionTimeToCurveCommand = new InsertConnectionCommand(compOp.Definition, timeToCurve, 0);

            var curveToCurrent = new MetaConnection(addCurveOpCommand.AddedInstanceID, curveMetaOp.Outputs[0].ID,
                                                    opPart.Parent.ID, opPart.ID);
            var connectionCurveToCurrentCommand = new InsertConnectionCommand(compOp.Definition, curveToCurrent, 0);

            var addKeyframeCommand = new AddOrUpdateKeyframeCommand(keyframeTime, currentValue, compOp, curveOpInstanceId, curveMetaOp.Inputs[0].ID);

            return new List<ICommand>() 
                        { 
                            setValueCommand,
                            addCurveOpCommand, 
                            addTimeOpCommand, 
                            connectionTimeToCurveCommand, 
                            connectionCurveToCurrentCommand,
                            addKeyframeCommand
                        };
        }
    }
}
