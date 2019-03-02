using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace TradingClient.BaseStyles.Converters
{
    public abstract class ValueOneWayConverter : ValueConverter
    {
        public override sealed object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException(); 
    }
}