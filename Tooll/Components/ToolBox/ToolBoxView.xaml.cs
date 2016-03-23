// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Framefield.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for ToolBoxView.xaml
    /// </summary>
    public partial class ToolBoxView : UserControl
    {
        public ToolBoxView() {
            InitializeComponent();

            App.Current.Model.MetaOpManager.ChangedEvent += (o, a) => UpdateMetaOpControls();
            UpdateMetaOpControls();
        }

        private void UpdateMetaOpControls() {
            MainPanel.Children.Clear();
            foreach (var metaOpEntry in App.Current.Model.MetaOpManager.MetaOperators)
                MainPanel.Children.Add(new OperatorTypeButton(metaOpEntry.Value));
        }

    }
}
