// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using SharpDX.RawInput;

namespace Framefield.Core.Inputs
{

    public class HidInput : IDisposable
    {
        public class HidEventArgs : EventArgs
        {
            public int Count { get; set; }
            public int DataSize { get; set; }
            public byte[] RawData { get; set; }

            public HidEventArgs(HidInputEventArgs e)
            {
                Count = e.Count;
                DataSize = e.DataSize;
                RawData = e.RawData;
            }

            public HidEventArgs()
            {
            }
        }

        public delegate void HidInputDelegate(object o, HidEventArgs e);

        public event HidInputDelegate HidInputEvent;

        public HidInput()
        {
            Device.RawInput += HandleRawInput;
        }

        public void Dispose()
        {
            Device.RawInput -= HandleRawInput;
        }

        private void HandleRawInput(object sender, RawInputEventArgs e)
        {
            HidInputEventArgs hidArgs = e as HidInputEventArgs;
            if (hidArgs == null || HidInputEvent == null)
                return;
            HidInputEvent(sender, new HidEventArgs(hidArgs));
        }
    }

}
