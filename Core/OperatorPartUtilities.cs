// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framefield.Core
{
    public static class OperatorPartUtilities
    {
        public static float GetInputFloatValue(OperatorPart opPart)
        {
            if (opPart.Connections.Count == 0)
            {
                float newValue = 0;
                var floatValue = ((opPart.Func as Utilities.ValueFunction).Value as Float);
                if (floatValue != null)
                    newValue = floatValue.Val;
                return newValue;
            }
            else
            {
                return opPart.Connections[0].Eval(new OperatorPartContext()).Value;
            }
        }

        public static string GetInputTextValue(OperatorPart opPart)
        {
            if (opPart.Connections.Count == 0)
            {
                string text = "";
                var textValue = ((opPart.Func as Utilities.ValueFunction).Value as Text);
                if (textValue != null)
                    text = textValue.Val;
                return text;
            }
            else
            {
                return opPart.Connections[0].Eval(new OperatorPartContext()).Text;
            }
        }

        public static OperatorPart FindLowestUnconnectedOpPart(OperatorPart opPart, int maxDepth)
        {
            if (opPart == null)
                return null;
            if (maxDepth < 0)
                return null;

            if (opPart.Connections.Count == 0)
                return opPart;

            foreach (var input in opPart.Connections)
            {
                OperatorPart foundElement = FindLowestUnconnectedOpPart(input, maxDepth - 1);
                if (foundElement != null)
                    return foundElement;
            }
            return null;
        }
    }
}
