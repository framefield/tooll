// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for PresetGrid.xaml
    /// </summary>
    public partial class PresetGrid : UserControl
    {
        public PresetGrid() {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            XPreviewButton.IsChecked = App.Current.OperatorPresetManager.LivePreviewEnabled;

            var binding= new Binding() {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Source = App.Current.OperatorPresetManager,
                Path = new PropertyPath("CurrentOperatorPresets")
            };
            BindingOperations.SetBinding(XPresetGrid, ItemsControl.ItemsSourceProperty, binding);
        }

        private void SaveClicked_Handler(object sender, RoutedEventArgs e) {
            App.Current.OperatorPresetManager.SavePresetFromCurrentlyShownOperator();
        }

        private void XPreviewButton_OnChecked(object sender, RoutedEventArgs e)
        {
            App.Current.OperatorPresetManager.LivePreviewEnabled = true;
        }

        private void XPreviewButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            App.Current.OperatorPresetManager.LivePreviewEnabled = false;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            App.Current.OperatorPresetManager.UpdateAllThumbnails();
        }
    }
}
