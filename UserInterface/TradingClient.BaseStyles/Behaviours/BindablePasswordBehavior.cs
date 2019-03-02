using System;
using System.Reflection;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interactivity;

namespace TradingClient.BaseStyles.Behaviours
{
    public class BindablePasswordBehavior : Behavior<PasswordBox>
    {
        #region Members

        private bool _ignoreChanging;
        
        #endregion //Members

        #region Dependency property

        private static readonly PropertyMetadata MetaData = new PropertyMetadata(null, PasswordChanged);

        public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register(nameof(Password),
            typeof (string), typeof (BindablePasswordBehavior), MetaData);

        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        private static void PasswordChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) =>
            (dependencyObject as BindablePasswordBehavior)?.OnPasswordChanged();

        #endregion //Dependency property

        #region Overrides

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PasswordChanged += PasswordBoxOnPasswordChanged;
            OnPasswordChanged();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PasswordChanged -= PasswordBoxOnPasswordChanged;
        }

        #endregion //Overrides

        #region Helper Methods

        private void PasswordBoxOnPasswordChanged(object sender, RoutedEventArgs routedEventArgs) =>
            ChangedAction(() => Password = AssociatedObject.Password);

        private void OnPasswordChanged() =>
            ChangedAction(() =>
            {
                if (AssociatedObject != null)
                    AssociatedObject.Password = Password;
            });

        private void ChangedAction(Action currentAction)
        {
            if (_ignoreChanging || currentAction == null)
                return;

            _ignoreChanging = true;
            currentAction.Invoke();
            _ignoreChanging = false;
        }

        #endregion //Helper Methods
    }
}