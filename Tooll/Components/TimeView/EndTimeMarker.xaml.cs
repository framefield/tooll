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
    /// Interaction logic for EndTimeMarker.xaml
    /// </summary>
    public partial class EndTimeMarker : UserControl, IValueSnapAttractor
    {
        public EndTimeMarker()
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
            double distanceToTime = Math.Abs(time - TV.EndTime) * TV.TimeScale;
            if (distanceToTime < SNAP_THRESHOLD) {
                return new SnapResult() { SnapToValue=TV.EndTime, Force=distanceToTime };
            }
            return null;
        }
        #endregion


        #region moving event handlers
        private void OnDragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control) {
                TV.EndTime+= TV.XToTime(e.HorizontalChange) - TV.XToTime(0);

                double snapTime= TV.TimeSnapHandler.CheckForSnapping(TV.EndTime, this);
                if (!Double.IsNaN(snapTime)) {
                    TV.EndTime = snapTime;
                }

                if (TV.EndTime < TV.StartTime+ 1/60) {
                    TV.EndTime = TV.StartTime+ 1/60;
                }
            }
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
