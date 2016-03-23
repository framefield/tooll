// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using Framefield.Core;
using Framefield.Helper;

namespace Framefield.Tooll
{

    public interface IConnectionLineSource : IConnectableWidget
    {
        List<OperatorPart> Outputs { get; }
        List<Thumb> OutputThumbs { get; }
        List<ConnectionLine> ConnectionsOut { get; }
    }
}
