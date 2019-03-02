using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TradingClient.BaseStyles.Converters
{
    public class StringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value.ToString();
            try
            {
                var color = ColorConverter.ConvertFromString(str);
                return (Color)color;
            }
            catch (FormatException)
            {
                return Colors.Red;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }
    }
}