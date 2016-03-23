// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.Collections.Generic;

namespace Framefield.Tooll.Components.SearchForOpWindow.ResultFinders
{
    public class CurrentFinder : PathFinder
    {
        public CurrentFinder(ReplaceOperatorWindow window) : base(window, App.Current.MainWindow.CompositionView.CompositionGraphView.CompositionOperator)
        {
        }
    }
}
