using System;
using System.Globalization;
using System.Windows.Data;
using TradingClient.Data.Contracts;

namespace TradingClient.BaseStyles.Converters
{
    [ValueConversion(typeof (OrderType), typeof (bool))]
    public class IsOrderLimitOrStopConverter : ValueOneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is OrderType type && (type == OrderType.Limit || type == OrderType.Stop);
    }
}