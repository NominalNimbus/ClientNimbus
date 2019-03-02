using System;
using System.Globalization;

namespace TradingClient.BaseStyles.Converters
{
    public class InverseBoolConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool boolValue ? !boolValue : true;

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Convert(value, targetType, parameter, culture);
    }
}