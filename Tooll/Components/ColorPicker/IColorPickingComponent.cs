// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Framefield.Core;
using Framefield.Helper;

namespace Framefield.Tooll
{

    public interface IColorPickingComponent
    {
        void StartColorManipulation();
        void ManipulateColor(float r, float g, float b, float a);
        void EndColorManipulation();
        ColorPickerView LinkedColorPicker { get; set; }
    }
}
