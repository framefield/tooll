// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using SharpDX;

namespace Framefield.Core
{
    public static class D3DDevice
    {
        public static SharpDX.Direct3D11.Device Device = null;//new Device(DriverType.Hardware, DeviceCreationFlags.None | DeviceCreationFlags.BgraSupport, FeatureLevel.Level_10_0);
        public static SharpDX.Direct3D10.Device1 DX10_1Device = null;
        public static SharpDX.Direct2D1.Factory Direct2DFactory = null;
        public static SharpDX.DirectWrite.Factory DirectWriteFactory = null;
        public static SharpDX.Direct3D11.DeviceDebug DebugDevice = null;
        public static SharpDX.DXGI.SwapChain SwapChain = null;
        public static SharpDX.Size2 WindowSize;
        public static int TouchHeight;
        public static int TouchWidth;

        public delegate void BeginFrameDelegate();
        public static event BeginFrameDelegate BeginFrameEvent;


        public static void BeginFrame()
        {
            if (BeginFrameEvent != null)
                BeginFrameEvent();
        }

        public delegate void EndFrameDelegate();
        public static event EndFrameDelegate EndFrameEvent;
        public static void EndFrame()
        {
            if (EndFrameEvent != null)
                EndFrameEvent();
        }
    }
}
