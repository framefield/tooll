// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.IO;
using Framefield.Core;

namespace Framefield.Tooll
{
    /// <summary>
    /// Interaction logic for EndTimeMarker.xaml
    /// </summary>
    public partial class TimeImage : UserControl
    {
        public TimeImage() {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var imagePath = App.Current.ProjectSettings.TryGet("Soundtrack.ImagePath", string.Empty);
            SetTimelineImagePath(imagePath);
        }

        public void SetTimelineImagePath(string imagePath)
        {            
            if (File.Exists(imagePath))
            {                
                var spectrumBitmap = new BitmapImage();
                spectrumBitmap.BeginInit();
                spectrumBitmap.UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute);
                spectrumBitmap.CacheOption = BitmapCacheOption.OnLoad;
                spectrumBitmap.EndInit();
                XImage.Source = spectrumBitmap;
                XImage.Width = spectrumBitmap.PixelWidth/100.0;

                App.Current.ProjectSettings["Soundtrack.ImagePath"] = imagePath;
            }
            else
            {
                XImage.Source = null;
                Logger.Warn("Soundtrack image not found: '{0}'! Please adjust 'Config/ProjectSettings.json'.", imagePath);
            }
        }

        #region dirty stuff
        private TimeView m_TV;
        public TimeView TV {
            get {
                if (m_TV == null)
                    m_TV = UIHelper.FindParent<TimeView>(this);
                return m_TV;
            }
        }
        #endregion
    }

    #region Value converter

    public class TimeScaleToXConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Count() != 2 || values.Contains(DependencyProperty.UnsetValue)) {
                return 0.0;
            }

            double timeScale = (double)values[0];
            double timeOffset = (double)values[1];
            double x =( - timeOffset) * timeScale;
            return x;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TimeScaleDurationToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Count() != 2 || values.Contains(DependencyProperty.UnsetValue)) {
                return 0.0;
            }

            double timeScale = (double)values[0];
            double timeOffset = (double)values[1];
            double width = timeScale;
            return  width;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    #endregion
}
