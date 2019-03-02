using System;
using System.Globalization;
using System.Windows.Media;
using TradingClient.Data.Contracts;

namespace TradingClient.BaseStyles.Converters
{
	public class OrderSideToForegroundConverter : ValueOneWayConverter
	{
		public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
            if (!(value is Side side))
                return null;

            return side == Side.Buy ? Brushes.GreenYellow : Brushes.Tomato;
		}
	}
}