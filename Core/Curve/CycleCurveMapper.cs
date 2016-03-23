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
    public class CycleCurveMapper : IOutsideCurveMapper
    {
        public void Calc(double u, SortedList<double, VDefinition> curveElements, out double newU, out double offset)
        {
            offset = 0.0;
            if (curveElements.Count < 2)
            {
                newU = u;
            }
            else
            {
                double firstU = curveElements.First().Key;
                double lastU = curveElements.Last().Key;
                double delta = 0.0;

                if (u < firstU)
                {
                    delta = firstU - u;
                    newU = lastU - (delta % (lastU - firstU));
                }
                else if (u > lastU)
                {
                    delta = u - lastU;
                    newU = firstU + (delta % (lastU - firstU));
                }
                else
                {
                    newU = u;
                }
            }
        }
    };

}