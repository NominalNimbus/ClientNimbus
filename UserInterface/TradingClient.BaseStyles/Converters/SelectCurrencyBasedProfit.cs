using System;
using System.Globalization;
using System.Windows.Data;
using TradingClient.Data.Contracts;

namespace TradingClient.BaseStyles.Converters
{
    public class SelectCurrencyBasedProfit : MultiValueConverter
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if(values.Length != 3)
                throw new ArgumentException();

            var currency = values[1] as string;
            var coefficient = values[0] as CurrencyBasedCoefficient;

            if (string.IsNullOrEmpty(currency) || coefficient == null || !(values[2] is decimal))
                return 0;

            var value = (decimal)values[2];

            switch (currency)
            {
                case "EUR":
                    return value * coefficient.EUR;
                case "USD":
                    return value * coefficient.USD;
                case "GBP":
                    return value * coefficient.GBP;
                default: throw new ArgumentException();
            }
        }
    }
}