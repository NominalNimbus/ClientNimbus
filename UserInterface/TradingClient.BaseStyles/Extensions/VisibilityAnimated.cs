using System.Windows;

namespace TradingClient.BaseStyles.Extensions
{
    public class VisibilityEx
    {
        public static readonly DependencyProperty VisibilityAnimatedProperty =
            DependencyProperty.RegisterAttached("VisibilityAnimated", 
                typeof(Visibility), typeof(VisibilityEx), new PropertyMetadata(default(Visibility)));

        public static void SetVisibilityAnimated(UIElement element, Visibility value)
        {
            element.SetValue(VisibilityAnimatedProperty, value);
        }

        public static Visibility GetVisibilityAnimated(UIElement element)
        {
            return (Visibility)element.GetValue(VisibilityAnimatedProperty);
        }
    }
}
