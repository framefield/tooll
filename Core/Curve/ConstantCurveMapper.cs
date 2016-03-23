// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framefield.Core.Curve
{
    public class ConstantCurveMapper : IOutsideCurveMapper
    {
        public void Calc(double u, SortedList<double, VDefinition> curveElements, out double newU, out double offset)
        {
            newU = u;
            offset = 0.0;
        }
    };

}