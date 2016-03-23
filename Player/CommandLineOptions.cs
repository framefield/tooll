// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Framefield.Player
{
    public class CommandLineOptions
    {
        public CommandLineOptions(IEnumerable<string> args)
        {
            var options = from possibleOption in args
                          where possibleOption.StartsWith("--")
                          select possibleOption.Substring(2).ToLower();

            foreach (var option in options)
            {
                switch (option)
                {
                    case "hide_dialog": HideDialog = true; break;
                    case "time_logging": TimeLoggingEnabled = true; break;
                }
            }
        }

        public bool HideDialog { get; private set; }
        public bool TimeLoggingEnabled { get; private set; }
    }
}
