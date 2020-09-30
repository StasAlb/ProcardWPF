using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Data;

namespace Common
{
    [ValueConversion(typeof(double), typeof(string))]
    public class DoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => ((double)value).ToString("0.000");

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double result;
            if (Double.TryParse(value?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out result))
                return result;
            return value;
        }
    }
}
