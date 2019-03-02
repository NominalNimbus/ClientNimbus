using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.Primitives;
using Selector = System.Windows.Controls.Primitives.Selector;

namespace TradingClient.BaseStyles.Converters
{
    internal static class EditorConverterHelper
    {
        public static IntegerUpDown GetIntegerEditor(Binding valuePropertyBinding, int minValue = Int32.MinValue, int maxValue = Int32.MaxValue, bool isReadOnly = false)
        {
            var control = new IntegerUpDown();
            SetNumericProperties(control, minValue, maxValue, isReadOnly);
            control.SetBinding(IntegerUpDown.ValueProperty, valuePropertyBinding);
            return control;
        }

        public static DoubleUpDown GetDoubleEditor(Binding valuePropertyBinding, double minValue = Double.MinValue, double maxValue = Double.MaxValue, bool isReadOnly = false)
        {
            var control = new DoubleUpDown();
            SetNumericProperties(control, minValue, maxValue, isReadOnly);
            control.SetBinding(DoubleUpDown.ValueProperty, valuePropertyBinding);
            return control;
        }

        public static TextBox GetTextEditor(Binding valuePropertyBinding)
        {
            var control = new TextBox();
            SetControlStyle(control);
            control.Foreground = Brushes.White;
            control.SetBinding(TextBox.TextProperty, valuePropertyBinding);
            return control;
        }

        public static ComboBox GetBoolComboboxEditor(Binding valuePropertyBinding) =>
            GetComboboxEditor(valuePropertyBinding, new string[] { "true", "false" });

        public static ComboBox GetComboboxEditor(Binding valuePropertyBinding, IEnumerable<string> values)
        {
            var control = new ComboBox();
            SetControlStyle(control);
            foreach (var val in values)
            {
                control.Items.Add(val);
            }

            control.SetBinding(Selector.SelectedItemProperty, valuePropertyBinding);
            return control;
        }

        public static CheckBox GetBoolCheckBoxEditor(Binding valuePropertyBinding)
        {
            var control = new CheckBox();
            SetControlStyle(control);
            control.SetBinding(CheckBox.IsCheckedProperty, valuePropertyBinding);
            control.HorizontalAlignment = HorizontalAlignment.Right;
            control.Padding = new Thickness(0);
            return control;
        }
            
        private static void SetNumericProperties<T>(CommonNumericUpDown<T> control, T minValue, T maxValue, bool isReadOnly) 
            where T : struct, IFormattable, IComparable<T>
        {
            SetControlStyle(control);
            control.Minimum = minValue;
            control.Maximum  = maxValue;
            control.IsReadOnly = isReadOnly;
        }

        private static void SetControlStyle(Control control)
        {
            control.Background = Brushes.Transparent;
            control.BorderThickness = new Thickness(0);
            control.BorderBrush = Brushes.Transparent;
            control.Margin = new Thickness(3);
        }
    }
}
