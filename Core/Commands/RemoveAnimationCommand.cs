// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framefield.Core.Curve;

namespace Framefield.Core.Commands
{

    public class RemoveAnimationCommand : MacroCommand
    {
        private static Guid CurveID = new Guid("145c3a6b-b91f-450d-ac46-b13c66ebce19");
        private static Guid CurrentTimeID = new Guid("253e302b-8141-4d17-96ee-42af092dbf59");

        public RemoveAnimationCommand() { }

        public RemoveAnimationCommand(OperatorPart opPart, float lastValue)
        {
            _name = "Remove Animation";
            _commands = new List<ICommand>();

            OperatorPart curveOpPart = Animation.FindOpPartWithFunctorTypeInSubTree<ICurve>(opPart, 3);
            OperatorPart timeOpPart = Animation.FindOpPartWithFunctorTypeInSubTree<OperatorPartTraits.ITimeAccessor>(curveOpPart, 3);

            if (curveOpPart != null && timeOpPart != null)
            {
                Operator compositionOp = opPart.Parent.Parent;
                _commands.Add(new DeleteOperatorsCommand(compositionOp, new List<Operator>() { curveOpPart.Parent, timeOpPart.Parent }));
                _commands.Add(new SetFloatValueCommand(opPart, lastValue));
            }
        }
    }

}
