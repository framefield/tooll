// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using SharpDX;

namespace Framefield.Core.OperatorPartTraits
{

    public interface ITimeClip
    {
        double StartTime { get; set; }
        double EndTime { get; set; }
        double SourceStartTime { get; set; }
        double SourceEndTime { get; set; }
        int Layer { get; set; }
    }

}
