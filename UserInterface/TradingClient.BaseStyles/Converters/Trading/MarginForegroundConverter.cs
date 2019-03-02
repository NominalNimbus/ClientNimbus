using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TradingClient.BaseStyles.Converters
{
    public class MarginForegroundConverter : ValueOneWayConverter
    {
        private Brush MarginBrush = Brushes.LightGray;
        private Brush NotMarginBrush = new SolidColorBrush(Color.FromArgb(255, 100,100,100));

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool isMarginAccount))
                return null;

            return isMarginAccount ? MarginBrush: NotMarginBrush;
        }
    }
}
