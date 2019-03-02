using System;
using System.Globalization;
using System.Windows.Media;

namespace TradingClient.BaseStyles.Converters
{
    public class ExecutionSideToBrushConverter : ValueOneWayConverter
    {
        public ExecutionSideToBrushConverter()
        {
            Server = Broker = Unknown = Brushes.Transparent;
        }

        public Brush Server { get; set; }

        public Brush Broker { get; set; }

        public Brush Unknown { get; set; }

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Unknown;

            if (!(value is bool))
                return Unknown;

            var side = (bool)value;
            return side
                       ? Server
                       : Broker;

        }
    }
}