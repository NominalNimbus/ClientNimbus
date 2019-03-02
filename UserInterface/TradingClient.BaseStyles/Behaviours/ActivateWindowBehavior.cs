using System.Windows;
using System.Windows.Interactivity;

namespace TradingClient.BaseStyles.Behaviours
{
    public class ActivateWindowBehavior : Behavior<Window>
    {
        private bool _isActivated;

        public static readonly DependencyProperty ActivatedProperty =
          DependencyProperty.Register(
            "Activated",
            typeof(bool),
            typeof(ActivateWindowBehavior),
            new PropertyMetadata(OnActivatedChanged)
          );

        public bool Activated
        {
            get { return (bool)GetValue(ActivatedProperty); }
            set { SetValue(ActivatedProperty, value); }
        }

        static void OnActivatedChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (ActivateWindowBehavior)dependencyObject;
            if (!behavior.Activated || behavior._isActivated)
                return;
            
            if (behavior.AssociatedObject.WindowState == WindowState.Minimized)
                behavior.AssociatedObject.WindowState = WindowState.Normal;
            behavior.AssociatedObject.Activate();
        }

        protected override void OnAttached()
        {
            AssociatedObject.Activated += OnActivated;
            AssociatedObject.Deactivated += OnDeactivated;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Activated -= OnActivated;
            AssociatedObject.Deactivated -= OnDeactivated;
        }

        void OnActivated(object sender, System.EventArgs eventArgs)
        {
            this._isActivated = true;
            Activated = true;
        }

        void OnDeactivated(object sender, System.EventArgs eventArgs)
        {
            this._isActivated = false;
            Activated = false;
        }
    }
}