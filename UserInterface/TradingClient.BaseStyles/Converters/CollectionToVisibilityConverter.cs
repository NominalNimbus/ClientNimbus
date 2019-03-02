using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TradingClient.BaseStyles.Converters
{
    [ValueConversion(typeof (ICollection), typeof (Visibility))]
    public class CollectionToVisibilityConverter : ValueOneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Collapsed;

            if (!(value is ICollection))
                throw new ArgumentException();

            return ((ICollection)value).Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}