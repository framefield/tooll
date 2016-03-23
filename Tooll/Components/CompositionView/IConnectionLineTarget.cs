// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.Collections.Generic;
using System.Windows;
using Framefield.Core;
using Framefield.Tooll.Components.CompositionView;

namespace Framefield.Tooll
{

    public interface IConnectionLineTarget : IConnectableWidget
    {
        List<OperatorPart> Inputs { get; }
        List<OperatorPart> VisibleInputs { get; }

        Rect GetRangeForInputConnectionLine(OperatorPart input, int multiInputIndex, bool insertConnection = false);

        void ClearHighlightInput();
        List<ConnectionLine> ConnectionsIn { get; }
        void UpdateInputZonesUIFromDescription(IEnumerable<OperatorWidgetInputZone> inputZones);
    }
}
