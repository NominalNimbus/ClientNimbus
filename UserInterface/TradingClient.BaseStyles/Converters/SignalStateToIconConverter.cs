using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using TradingClient.Data.Contracts;

namespace TradingClient.BaseStyles.Converters
{
    [ValueConversion(typeof (State), typeof (BitmapImage))]
    public class SignalStateToIconConverter : ValueOneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (!(value is State))
                throw new ArgumentException();

            var URI = UriKind.RelativeOrAbsolute;
            var state = (State)value;
            switch (state)
            {
                case State.New: return new BitmapImage(new Uri("../Resources/Images/Signal/unknown12.png", URI));
                case State.Stopped: return new BitmapImage(new Uri("../Resources/Images/Signal/stopped12.png", URI));
                case State.Paused: return new BitmapImage(new Uri("../Resources/Images/Signal/paused12.png", URI));
                case State.Working: return new BitmapImage(new Uri("../Resources/Images/Signal/working12.png", URI));
                case State.Backtesting: return new BitmapImage(new Uri("../Resources/Images/Signal/backtest12.png", URI));
                case State.BacktestPaused: return new BitmapImage(new Uri("../Resources/Images/Signal/backtestpaused12.png", URI));
                default: return new BitmapImage();
            }
        }
    }
}