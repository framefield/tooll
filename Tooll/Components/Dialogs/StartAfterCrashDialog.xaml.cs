// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Xml.Linq;

namespace Framefield.Tooll.Components.Dialogs

{
    public partial class StartAfterCrashDialog : Window
    {
        public StartAfterCrashDialog(double secondsSinceLastCrash = 0)
        {
            InitializeComponent();
            XTimeTextBlock.Text = secondsSinceLastCrash > 0
                ? String.Format("A backup was saved {0} minutes ago. Do you want to restore it?", (int)(secondsSinceLastCrash/60)) 
                : "Sadly no backup was created since the last startup.";

            if (secondsSinceLastCrash == 0)
            {
                XRestoreButton.Visibility = Visibility.Collapsed;
            }
        }


        void OkButtonHandler(object sender, RoutedEventArgs e) {
            DialogResult = true;
            Close();
        }

        void CancelButtonHandler(object sender, RoutedEventArgs e) {
            DialogResult = false;
            Close();
        }

        // Allow dragging the dialog not only at the titlebar
        private void Grid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
          DragMove();
        }

        // Hide the close-buttom from the titlebar
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }    
    }
}
