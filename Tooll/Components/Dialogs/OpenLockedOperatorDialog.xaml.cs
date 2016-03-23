// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Framefield.Tooll.Components.Dialogs

{
    public partial class OpenLockedOperatorDialog : Window
    {
        public OpenLockedOperatorDialog()
        {
            InitializeComponent();
        }

        void OkButtonHandler(object sender, RoutedEventArgs e) 
        {
            DialogResult = true;
            DontAskAgain = XCheckBox.IsChecked == true;
        }

        void CancelButtonHandler(object sender, RoutedEventArgs e) 
        {
            DialogResult = false;
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

        public bool DontAskAgain { get; set; }
    }
}
