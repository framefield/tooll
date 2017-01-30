// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Windows;
using System.Windows.Interop;
using Framefield.Core;
using Framefield.Core.Inputs;
using Logger = Framefield.Core.Logger;

namespace Framefield.Tooll.Components.Helper
{
    /**
     *  Creates a 3DX-Mice handler and connects it to the MainWindows message sink.
     *  To get updates on 3d-mice events, connect to...
     *  
     *   Active3DxMouse.MotionEvent += MotionEventHandler;
     *   Active3DxMouse.ButtonEvent += ButtonEventHandler;
     */
    public class SpaceMouseWPFHandler
    {
        public SpaceMouse Active3DxMouse { get; set; }

        public SpaceMouseWPFHandler()
        {
            IntPtr hwnd = IntPtr.Zero;
            Window myWin = Application.Current.MainWindow;

            try
            {
                hwnd = new WindowInteropHelper(myWin).Handle;
            }
            catch (Exception ex)
            {
                Logger.Error("Setting up 3D-Mice failed:", ex);
            }

            //Get the Hwnd source   
            var hwndSource = HwndSource.FromHwnd(hwnd);
            if (hwndSource == null)
                return;

            hwndSource.AddHook(Win32QueueSinkHandler);

            // Connect to Raw Input & find devices
            Active3DxMouse = new SpaceMouse(hwndSource.Handle);

            // SetupContextForRenderingCamToBuffer event handlers to be called when something happens
            //Active3DxMouse.MotionEvent += MotionEventHandler;
            //Active3DxMouse.ButtonEvent += ButtonEventHandler;

            InitDeviceList();
        }

        private void InitDeviceList()
        {
            foreach (System.Collections.DictionaryEntry listEntry in Active3DxMouse.deviceList)
            {
                SpaceMouse.DeviceInfo devInfo = listEntry.Value as SpaceMouse.DeviceInfo;
            }
        }

        public IntPtr Win32QueueSinkHandler(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (Active3DxMouse != null)
            {
                // I could have done one of two things here.
                // 1. Use a Message as it was used before.
                // 2. Changes the ProcessMessage method to handle all of these parameters(more work).
                //    I opted for the easy way.

                //Note: Depending on your application you may or may not want to set the handled param.

                _message.HWnd = hwnd;
                _message.Msg = msg;
                _message.LParam = lParam;
                _message.WParam = wParam;

                Active3DxMouse.ProcessMessage(_message);
            }
            return IntPtr.Zero;
        }

        private System.Windows.Forms.Message _message;
    }
}
