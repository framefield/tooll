// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framefield.Core.Commands
{
    public class ResetInputToGroupCommand : MacroCommand
    {
        public ResetInputToGroupCommand() { }

        public class Entry 
        {
            public OperatorPart OpPart;
            public IValue Value;
        }

        public ResetInputToGroupCommand(IEnumerable<OperatorPart> opParts)
        {
            _name = "Reset parameter group";

            foreach (var opPart in opParts) 
            {
                OperatorPart animationOpPart = Animation.GetRegardingAnimationOpPart(opPart);
                var isAnimated = animationOpPart != null;
                var isConnected = !isAnimated && opPart.Connections.Count > 0;

                // Don't reset connected parameters that are not animations
                if (!isConnected)
                {
                    if (isAnimated)
                    {
                        var removeAnimationCmd = new RemoveAnimationCommand(opPart, 0); // Last Value will be restored anyways
                        _commands.Add(removeAnimationCmd);
                    }

                    var resetCmd = new ResetInputToDefault(opPart);
                    _commands.Add(resetCmd);
                }                    
            }
        }
    }
}
