// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Windows.Input;

namespace Framefield.Tooll.Utils
{
    public class SetWaitCursor : IDisposable
    {
        public SetWaitCursor()
        {
            _previousCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = Cursors.Wait;
        }

        public void Dispose()
        {
            Mouse.OverrideCursor = _previousCursor;
        }

        readonly Cursor _previousCursor;
    }
}
