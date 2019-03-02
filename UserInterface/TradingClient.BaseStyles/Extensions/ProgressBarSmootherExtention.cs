using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace TradingClient.BaseStyles.Extensions
{
    public class ProgressBarSmootherExtention
    {
        public static double GetSmoothValue(DependencyObject obj)
        {
            return obj == null ? 0.0 : (double)obj.GetValue(SmoothValueProperty);
        }

        public static void SetSmoothValue(DependencyObject obj, double value)
        {
            obj.SetValue(SmoothValueProperty, value);
        }

        public static readonly DependencyProperty SmoothValueProperty = DependencyProperty.RegisterAttached("SmoothValue", 
            typeof(double), typeof(ProgressBarSmootherExtention), new PropertyMetadata(0.0, Changing));

        private static void Changing(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var anim = new DoubleAnimation((double)e.OldValue, (double)e.NewValue, System.TimeSpan.FromMilliseconds(150));
            (obj as ProgressBar).BeginAnimation(ProgressBar.ValueProperty, anim, HandoffBehavior.Compose);
        }
    }
}