using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace TradingClient.BaseStyles.Converters
{
    public class BalanceConverter : MultiValueConverter
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length == 2 && values[0] is decimal value && values[1] is int decimals)
            {
                return value.ToString($"N{decimals}");
            }
            else
                return "0";
        }
    }
}
