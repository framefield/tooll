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
    public class ConstInterpolator
    {
        public static void UpdateTangents(List<KeyValuePair<double, VDefinition>> curveElements) { }

        public static double Interpolate(KeyValuePair<double, VDefinition> a, KeyValuePair<double, VDefinition> b, double u)
        {
            return a.Value.Value;
        }
    };

}
