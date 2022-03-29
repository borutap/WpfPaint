using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfPaint
{
    class SliderToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not double)
                return null;
            string ret = Math.Ceiling((double)value).ToString();
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    class AngleToMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string)
                return null;
            string val = (string)value;
            Thickness ret = new Thickness(38 - val.Length * 3, 75, 0, 0);
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
