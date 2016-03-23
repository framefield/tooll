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
    public interface ICurve : IComparable
    {
        event EventHandler<System.EventArgs> ChangedEvent;

        Utils.OutsideCurveBehavior PreCurveMapping { get; set; }
        Utils.OutsideCurveBehavior PostCurveMapping { get; set; }

        bool ChangedEventEnabled { get; set; }

        bool HasVAt(double u);
        bool ExistVBefore(double u);
        bool ExistVAfter(double u);

        double? GetPreviousU(double u);
        double? GetNextU(double u);

        void AddOrUpdateV(double u, VDefinition v);
        void RemoveV(double u);
        void MoveV(double u, double newU);
        List<KeyValuePair<double, VDefinition>> GetPoints();

        VDefinition GetV(double u);
        double GetSampledValue(double u);
    }

}