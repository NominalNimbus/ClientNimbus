using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using TradingClient.Common;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.Primitives;
using Selector = System.Windows.Controls.Primitives.Selector;

namespace TradingClient.BaseStyles.Converters
{
    public class ParameterToEditorConverter : ValueOneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            if (value == null)
                return null;

            var valueBinding = new Binding("Value") {UpdateSourceTrigger = UpdateSourceTrigger.LostFocus};

            switch (value)
            {
                case IntParam intp:
                    return EditorConverterHelper.GetIntegerEditor(valueBinding, intp.MinValue, intp.MaxValue, intp.IsReadOnly);
                case DoubleParam doubleP:
                    return EditorConverterHelper.GetDoubleEditor(valueBinding, doubleP.MinValue, doubleP.MaxValue, doubleP.IsReadOnly);
                case StringParam stringP:
                    if (stringP.AllowedValues.Count > 0)
                        return EditorConverterHelper.GetComboboxEditor(valueBinding, stringP.AllowedValues);
                    else
                        return EditorConverterHelper.GetTextEditor(valueBinding);
                case BoolParam _:
                    return EditorConverterHelper.GetBoolCheckBoxEditor(valueBinding);
            }

            return null;
        }
    }


    public class ParameterByTypeToEditorConverter : ValueOneWayConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            var valueBinding = new Binding("Value") { UpdateSourceTrigger = UpdateSourceTrigger.LostFocus };

            switch(value)
            {
                case int _:
                    return EditorConverterHelper.GetIntegerEditor(valueBinding);
                case double _:
                    return EditorConverterHelper.GetDoubleEditor(valueBinding);
                case string _:
                    return EditorConverterHelper.GetTextEditor(valueBinding);
                case bool _:
                    return EditorConverterHelper.GetBoolCheckBoxEditor(valueBinding);
            }

            return null;
        }
    }
}