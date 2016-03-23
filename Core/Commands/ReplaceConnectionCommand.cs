// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Framefield.Core.Commands
{
    public class ReplaceConnectionCommand : MacroCommand
    {
        public ReplaceConnectionCommand() {}

        public ReplaceConnectionCommand(Operator op, Connection connection)
        {
            _name = "Replace Connection";

            var prevSourceOpPart = connection.TargetOpPart.Connections[connection.Index];
            var opResult = op.InternalOps.Find(innerOp => innerOp.Outputs.Exists(opPart => opPart == prevSourceOpPart));
            var prevSourceOp = (opResult == null) ? op : opResult;

            var connectionToRemove = new Connection(prevSourceOp, prevSourceOpPart, connection.TargetOp, connection.TargetOpPart, connection.Index);
            _commands =  new List<ICommand>() {new RemoveConnectionCommand(op, connectionToRemove),
                                               new InsertConnectionCommand(op, connection) };
        }
    }

}
