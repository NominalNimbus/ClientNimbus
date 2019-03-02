using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using TradingClient.Data.Contracts;
using Xceed.Wpf.Toolkit;

namespace TradingClient.BaseStyles.Converters
{
    public class AnalyserParameterToEditorConverter : ValueOneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            var valueBinding = new Binding(parameter?.ToString() ?? "StartValue")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
            };

            switch (value)
            {
                case IntParam intParam:
                    return EditorConverterHelper.GetIntegerEditor(valueBinding, intParam.MinValue, intParam.MaxValue, intParam.IsReadOnly);
                case DoubleParam doubleParam:
                    return EditorConverterHelper.GetDoubleEditor(valueBinding, doubleParam.MinValue, doubleParam.MaxValue, doubleParam.IsReadOnly);
                default:
                    return new TextBlock
                    {
                        Background = Brushes.Transparent,
                        Foreground = Brushes.Gray,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Text = "N/A"
                    };
            }
        }

    }
}