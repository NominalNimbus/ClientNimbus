using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace TradingClient.BaseStyles.Converters
{
    public class EmptyValuePresenterConverter : ValueOneWayConverter
    {
        public string ZeroEqualsPresentation { get; set; }

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double)
            {
                if ((double)value == 0)
                    return ZeroEqualsPresentation;
            }

            else if (value is int)
            {
                if ((int)value == 0)
                    return ZeroEqualsPresentation;
            }

            else if (value is decimal)
            {
                if ((decimal)value == 0)
                    return ZeroEqualsPresentation;
            }

            else if (value is long)
            {
                if ((long)value == 0)
                    return ZeroEqualsPresentation;
            }

            return value;
        }
    }
}