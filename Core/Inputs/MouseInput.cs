// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;

namespace Framefield.Core.Inputs
{

    public class MouseInput : IDisposable
    {
        [Flags]
        public enum ButtonFlags
        {
            None = SharpDX.RawInput.MouseButtonFlags.None,
            LeftButtonDown = SharpDX.RawInput.MouseButtonFlags.LeftButtonDown,
            LeftButtonUp = SharpDX.RawInput.MouseButtonFlags.LeftButtonUp,
            MiddleButtonDown = SharpDX.RawInput.MouseButtonFlags.MiddleButtonDown,
            MiddleButtonUp = SharpDX.RawInput.MouseButtonFlags.MiddleButtonUp,
            RightButtonDown = SharpDX.RawInput.MouseButtonFlags.RightButtonDown,
            RightButtonUp = SharpDX.RawInput.MouseButtonFlags.RightButtonUp,
            Wheel = SharpDX.RawInput.MouseButtonFlags.MouseWheel
        }

        public class MouseEventArgs : EventArgs
        {
            public ButtonFlags ButtonFlags { get; set; }
            public int ExtraInformation { get; set; }
            public int WheelDelta { get; set; }
            public int X { get; set; }
            public int Y { get; set; }

            public MouseEventArgs(SharpDX.RawInput.MouseInputEventArgs e)
            {
                ButtonFlags = (ButtonFlags) e.ButtonFlags;
                ExtraInformation = e.ExtraInformation;
                WheelDelta = e.WheelDelta;
                X = e.X;
                Y = e.Y;
            }

            public MouseEventArgs()
            {
            }
        }

        public delegate void MouseInputDelegate(object o, MouseEventArgs e);

        public event MouseInputDelegate MouseInputEvent;

        public MouseInput()
        {
            SharpDX.RawInput.Device.MouseInput += HandleMouseInput;
        }

        public void Dispose()
        {
            SharpDX.RawInput.Device.MouseInput -= HandleMouseInput;
        }

        private void HandleMouseInput(object sender, SharpDX.RawInput.MouseInputEventArgs e)
        {
            if (MouseInputEvent != null)
                MouseInputEvent(sender, new MouseEventArgs(e));
        }
    }

}
