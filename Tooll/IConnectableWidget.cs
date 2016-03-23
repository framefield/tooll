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
    /// <summary>
    /// Base class for...
    ///   - IConnectionLineTarget
    ///     - OutputWidget
    ///     - OperatorWidget
    ///     - InputWidget
    /// </summary>
    public interface IConnectableWidget : ISelectable
    {
        CompositionView CV { get; }

        /*
         * For output- and Input-Widgets we have to map their Position in 
         * their region to the OpWidget-Canvas to draw proper connections
         */
        Point PositionOnCanvas { get; }

        void UpdateConnections();
    }

}
