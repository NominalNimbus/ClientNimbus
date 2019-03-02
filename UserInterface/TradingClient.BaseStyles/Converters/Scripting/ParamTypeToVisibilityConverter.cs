using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using TradingClient.Data.Contracts;

namespace TradingClient.BaseStyles.Converters
{
    public class ParamTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value != null && (value is IntParam || value is DoubleParam))
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}