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
using Framefield.Tooll;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for Playhead.xaml
    /// 
    /// Note the binding of playhead to the currentTime property is done in TimeView.CreateBindings()
    /// </summary>
    public partial class Playhead : UserControl, IValueSnapAttractor
    {
        public Playhead()
        {
            InitializeComponent();
        }


        #region implement snap attractor
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (TV != null) {
                TV.TimeSnapHandler.AddSnapAttractor(this);
            }
        }


        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (TV != null) {
                TV.TimeSnapHandler.RemoveSnapAttractor(this);
            }
        }

        const double SNAP_THRESHOLD = 8;


        public SnapResult CheckForSnap(double time)
        {
            double distanceToTime = Math.Abs(time - App.Current.Model.GlobalTime) * TV.TimeScale;
            if (distanceToTime < SNAP_THRESHOLD) {
                return new SnapResult() { SnapToValue = App.Current.Model.GlobalTime, Force = distanceToTime };
            }
            return null;
        }
        #endregion



        #region moving event handlers
        private void OnDragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            double delta = TV.XToTime(e.HorizontalChange) - TV.XToTime(0);
            double currentTime = App.Current.Model.GlobalTime + delta;

            if (Keyboard.Modifiers == ModifierKeys.Shift) {
                double snapTime = TV.TimeSnapHandler.CheckForSnapping(currentTime, this);
                if (!Double.IsNaN(snapTime)) {
                    currentTime = snapTime;
                }
            }
            App.Current.MainWindow.CompositionView.XCompositionToolBar.ManipulateTime(currentTime);

        }
        #endregion


        #region dirty stuff
        private TimeView m_TV;
        public TimeView TV
        {
            get
            {
                if (m_TV == null)
                    m_TV = UIHelper.FindParent<TimeView>(this);
                return m_TV;
            }
        }
        #endregion

    }
}
