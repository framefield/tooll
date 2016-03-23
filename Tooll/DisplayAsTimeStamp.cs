// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Globalization;
using System.Windows.Data;

namespace Framefield.Tooll
{
    [ValueConversion(typeof(double), typeof(string))]
    class DisplayAsTimeStamp : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            double dValue = (double) value;

            return ((int) (dValue / 60 % 60)).ToString("D2") + ":" +
                   ((int) (dValue      % 60)).ToString("D2") + ":" +
                   ((int) (dValue * 30 % 30)).ToString("D2");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            double dValue;
            double.TryParse((string) value, NumberStyles.Float, CultureInfo.InvariantCulture, out dValue);
            return dValue;
        }
    }
}
