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

    public interface ISelectable
    {
        Point Position { get; set; }
        double Width { get; set; }
        double Height { get; set; }
        bool IsSelected { get; set; }
    }
}
