// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Threading;
using Framefield.Tooll;
using Framefield.Helper;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for TimeMarkerEditor.xaml
    /// </summary>
    public partial class TimeMarkerEditor : UserControl
    {
        public TimeMarkerEditor()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            if (App.Current != null) {
                var timeClipBinding = new Binding() {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Source = App.Current.MainWindow.CompositionView.CompositionGraphView,
                    Path = new PropertyPath("TimeMarkers")
                };

                BindingOperations.SetBinding(XItemsControl, ItemsControl.ItemsSourceProperty, timeClipBinding);
            }
        }

        private void OnDragTimelineDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e) {
            e.Handled = false;
            XItemsControl.RaiseEvent(e);
        }

        private void OnDragTimelineStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e) {
            e.Handled = false;
            XItemsControl.RaiseEvent(e);
        }
    }

}
