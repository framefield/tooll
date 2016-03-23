// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.Collections.Generic;

namespace Framefield.Tooll.Components.SearchForOpWindow.ResultFinders
{
    public abstract class ResultFinder
    {
        protected ReplaceOperatorWindow Window;

        protected ResultFinder(ReplaceOperatorWindow window)
        {
            Window = window;
        }

        public abstract void FindResults();
    }
}
