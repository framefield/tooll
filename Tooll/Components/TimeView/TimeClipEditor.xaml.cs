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
    /// Interaction logic for TimeClipEditor.xaml
    /// </summary>
    public partial class TimeClipEditor : UserControl
    {
        public TimeClipEditor()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            if (App.Current != null) {
                var timeClipBinding = new Binding() {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Source = App.Current.MainWindow.CompositionView.CompositionGraphView,
                    Path = new PropertyPath("TimeClips")
                };

                BindingOperations.SetBinding(XItemsControl, ItemsControl.ItemsSourceProperty, timeClipBinding);
            }
        }
    }


    #region Value converter
    public class TimeScaleOffsetToXConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Count() != 3 || values.Contains(DependencyProperty.UnsetValue)) {
                return 0.0;
            }

            double u = (double) values[0];
            double timeScale= (double) values[1];
            double timeOffset= (double) values[2];
            return (u - timeOffset) * timeScale;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DurationTimescaleToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Count() != 2 || values.Contains(DependencyProperty.UnsetValue)) {
                return 1.0;
            }

            double duration = (double) values[0];
            double timeScale= (double) values[1];
            double width = duration * timeScale;
            return width < 1.0 ?  1.0 : width;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LayerToYConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Count() != 1 || values.Contains(DependencyProperty.UnsetValue))
            {
                return 0.0;
            }

            int layer = (int)values[0];
            return (double)layer*21;

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    #endregion
}
