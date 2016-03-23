// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.Windows;

namespace Framefield.Tooll
{
    public partial class UserNameDialog : Window
    {
        public UserNameDialog() {
            InitializeComponent();
        }

        void OkButtonHandler(object sender, RoutedEventArgs e) {
            DialogResult = !string.IsNullOrEmpty(XUserName.Text);
            //DialogResult = !string.IsNullOrEmpty(XUserName.Text);
        }

        void CancelButtonHandler(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }
    }
}
