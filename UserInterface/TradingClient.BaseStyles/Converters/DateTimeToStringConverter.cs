using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace TradingClient.BaseStyles.Converters
{
    public class DateTimeToStringConverter : ValueOneWayConverter
    {
        public string Format { get; set; }

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DateTime date) || date == DateTime.MinValue || date == DateTime.MaxValue)
                return string.Empty;

            return string.IsNullOrEmpty(Format) ? date.ToString() : date.ToString(Format);
        }
    }
}