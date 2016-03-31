// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framefield.Core.Commands
{
    public class SetValueGroupCommand : MacroCommand
    {
        public SetValueGroupCommand() { }

        public UpdateOperatorPartValueFunctionCommand this[int idx]
        {
            get { return (UpdateOperatorPartValueFunctionCommand)_commands[idx]; }
        }

        public void UpdateFloatValueAtIndex(int index, float newValue)
        {
            var cmd = _commands[index];
           var  keyframeCmd = cmd as AddOrUpdateKeyframeCommand;
            if (keyframeCmd != null)
            {
                keyframeCmd.KeyframeValue.Value = newValue;
                return;
            }

            var valueCmd = cmd as UpdateOperatorPartValueFunctionCommand;
            if (valueCmd != null)
            {
                valueCmd.Value = new Float(newValue);
                return;
            }

            throw new Exception("undefined command in SetValueGroupCommand");
        }


        public class Entry 
        {
            public OperatorPart OpPart;
            public Float Value;
        }

        public SetValueGroupCommand(IEnumerable<Entry> entries, double globalTime, string name = "Set Value Group")
        {
            _name = name;

            foreach (var entry in entries)
            {
                ICommand cmd;

                var isControlAnimated = false;

                OperatorPart animationOpPart = Animation.GetRegardingAnimationOpPart(entry.OpPart);
                isControlAnimated = animationOpPart != null;

                if (isControlAnimated)
                {
                    cmd = new AddOrUpdateKeyframeCommand(globalTime, entry.Value.Val, entry.OpPart);
                }
                else
                {
                    cmd = new UpdateOperatorPartValueFunctionCommand(entry.OpPart, entry.Value);
                
                }
                _commands.Add(cmd);
            }
        }
    }
}
