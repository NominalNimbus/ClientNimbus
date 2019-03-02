using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingClient.Data.Contracts;

namespace TradingClient.BaseStyles.Converters
{
    public class OrderStatusToStringConverter : ValueOneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Status status))
                return string.Empty;

            switch (status)
            {
                case Status.Filled:
                    return "Filled";
                case Status.Canceled:
                    return "Canceled";
                default:
                    return string.Empty;
            }
        }
    }
}
