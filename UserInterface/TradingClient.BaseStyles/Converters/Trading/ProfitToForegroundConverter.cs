using System;
using System.Globalization;
using System.Windows.Media;

namespace TradingClient.BaseStyles.Converters
{
    public class ProfitToForegroundConverter : ValueOneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            decimal changeValue;
            switch (value)
            {
                case decimal decValue:
                    changeValue = decValue;
                    break;
                case double dblValue:
                    changeValue = (decimal)dblValue;
                    break;
                case int intValue:
                    changeValue = intValue;
                    break;
                default:
                    throw new NotSupportedException("Not supported value type");
            }

            if (changeValue > 0)
                return Brushes.GreenYellow;//#2b9c5c
            if (changeValue < 0)
                return Brushes.Tomato;//d70000

            return Brushes.WhiteSmoke;
        }
    }
}