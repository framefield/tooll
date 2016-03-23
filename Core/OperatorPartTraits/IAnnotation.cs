// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using SharpDX;

namespace Framefield.Core.OperatorPartTraits
{

    public interface IAnnotation
    {
        string Text { get; set; }
        double Height { get; set; }
        double FontSize { get; set; }
        Color4 Color { get; set; }
    }
}
