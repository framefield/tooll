// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using SharpDX;

namespace Framefield.Core.OperatorPartTraits
{
    public interface IMidiInput
    {
        string Device { get; }
        float Channel { get; }
        float Control { get; }
        float CurrentMidiValue { get; set; }
        float TargetMidiValue { get; set; }
        bool UsesPickup { get; }
        bool AllowPresets { get; }
    }
}