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
using Framefield.Core;

using Framefield.Core.OperatorPartTraits;
using Framefield.Core.Commands;
using ICommand = Framefield.Core.ICommand;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for CompositionToolBar.xaml
    /// </summary>
    public partial class CompositionToolBar : UserControl
    {
        public CompositionToolBar() {
            InitializeComponent();

            var timeBinding = new Binding() {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Source = App.Current.Model,
                Path = new PropertyPath("GlobalTime"),                
                Mode = BindingMode.TwoWay
            };
            XTimeEdit.SetBinding(FloatEditButton.ValueProperty, timeBinding);
            XTimeEdit.DisplayAsTimeStamp= true;
            XTimeEdit.Max=300.0f;
        }


        public void ManipulateTime(double time) {
            App.Current.Model.GlobalTime= time;
            App.Current.SetStreamToTime(time);
        }


        // TODO: This should be a bind to playback speed
        public void UpdatePlayIconColors() {
            CompositionView cv = GetCompositionView();
            if (cv.PlaySpeed > 0.0) {
                playReverseIconPath.Fill = new SolidColorBrush(Color.FromRgb(128, 128, 128));
                playReverseIconPath.Fill.Freeze();
                playIconPath.Fill = new SolidColorBrush(Color.FromRgb(255, 0xd6, 1));
                playIconPath.Fill.Freeze();
            }
            else if (cv.PlaySpeed == 0.0) {
                playReverseIconPath.Fill = playIconPath.Fill = new SolidColorBrush(Color.FromRgb(128, 128, 128));
                playReverseIconPath.Fill.Freeze();
            }
            else {
                playReverseIconPath.Fill = new SolidColorBrush(Color.FromRgb(255, 0xd6, 1));
                playReverseIconPath.Fill.Freeze();
                playIconPath.Fill = new SolidColorBrush(Color.FromRgb(128, 128, 128));
                playIconPath.Fill.Freeze();
            }

            if (cv.PlaySpeed > 1.0) {
                playSpeedText.Text = "value" + cv.PlaySpeed.ToString("F0");
            }
            else {
                playSpeedText.Text = "";
            }
        }

        CompositionView GetCompositionView() {
            return UIHelper.FindParent<CompositionView>(this);
        }

        private void OnSplitTimeClipClicked(object sender, RoutedEventArgs e)
        {
            Components.TimeView.TimeClipHelpers.SplitSelectedTimeClips();
        }

        private void OnLoopModeToggleClicked(object sender, RoutedEventArgs e)
        {
            if (App.Current.MainWindow.CompositionView.XTimeView.LoopMode) {
                App.Current.MainWindow.CompositionView.XTimeView.LoopMode = false;
                XLoopIconPath.Stroke= Brushes.Gray;
            }
            else {
                App.Current.MainWindow.CompositionView.XTimeView.LoopMode = true;
                XLoopIconPath.Stroke= Brushes.Orange;
                
            }
        }
    }
}

