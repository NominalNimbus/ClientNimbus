using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace TradingClient.BaseStyles.Converters
{
    public class PriceToStringConverter : MultiValueConverter
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length != 2 || !(values[0] is decimal price) || !(values[1] is int decimals) || price == 0)
                return string.Empty;

            return price.ToString($"0.{new string('0', decimals)}");
        }
    }
}