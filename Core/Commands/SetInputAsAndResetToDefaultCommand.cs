// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Newtonsoft.Json;


namespace Framefield.Core.Commands
{

    [JsonObject(MemberSerialization.OptIn)]
    public class SetInputAsAndResetToDefaultCommand : MacroCommand
    {
        public SetInputAsAndResetToDefaultCommand() {}

        public SetInputAsAndResetToDefaultCommand(OperatorPart input)
            : base("Set input as and reset to default", new List<ICommand> { new SetInputAsDefault(input), new ResetInputToDefault(input) })
        {
        }
    }

}
