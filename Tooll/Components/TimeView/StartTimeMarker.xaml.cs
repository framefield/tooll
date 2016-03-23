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
    /// Interaction logic for StartTimeMarker.xaml
    /// </summary>
    public partial class StartTimeMarker : UserControl, IValueSnapAttractor
    {
        public StartTimeMarker()
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
            double distanceToTime = Math.Abs(time - TV.StartTime) * TV.TimeScale;
            if (distanceToTime < SNAP_THRESHOLD) {
                return new SnapResult() { SnapToValue=TV.StartTime, Force=distanceToTime };
            }
            return null;
        }
        #endregion



        #region moving event handlers
        private void OnDragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control) {
                double delta = TV.XToTime(e.HorizontalChange) - TV.XToTime(0);

                TV.StartTime+= delta;

                double snapTime= TV.TimeSnapHandler.CheckForSnapping(TV.StartTime, this);
                if (!Double.IsNaN(snapTime)) {
                    TV.StartTime = snapTime;
                }

                if (TV.StartTime > TV.EndTime - 1/60) {
                    TV.StartTime = TV.EndTime- 1/60;
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

        public static TimeToLeftMarginConverter m_TimeToLeftMarginConverter = new TimeToLeftMarginConverter();
        public class TimeToLeftMarginConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (values.Count() != 4 || values.Contains(DependencyProperty.UnsetValue)) {
                    return "binding error";
                }

                double time = (double) values[0];
                double timeScale = (double) values[1];
                double timeOffset = (double) values[2];
                double width = (double) values[3];

                double x = (time - timeOffset) * timeScale + 2;
                return new Thickness(0, 0, width-x, 0);
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
                System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

    }
}
