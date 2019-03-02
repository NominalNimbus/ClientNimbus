using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace TradingClient.BaseStyles.Behaviours
{
    public class DialogResultBehavior : Behavior<Window>
    {
        #region Members

        private bool _allowExit;
        private bool _exitProcess;

        #endregion //Members

        #region Dependency Properties

        private static readonly PropertyMetadata DialogResultMetaData = new PropertyMetadata(null, DialogResultPropertyChangedCallback);

        public static readonly DependencyProperty DialogResultTriggerProperty =
            DependencyProperty.Register(nameof(DialogResultTrigger), typeof (bool?), typeof (DialogResultBehavior), DialogResultMetaData);

        public bool? DialogResultTrigger
        {
            get => (bool?)GetValue(DialogResultTriggerProperty);
            set => SetValue(DialogResultTriggerProperty, value);
        }

        private static void DialogResultPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((DialogResultBehavior)d).OnClose();

        private static readonly PropertyMetadata ClosingCommandMetaData = new PropertyMetadata(default(ICommand));

        public static readonly DependencyProperty ClosingCommandProperty = DependencyProperty.Register(nameof(ClosingCommand),
            typeof (ICommand), typeof (DialogResultBehavior), ClosingCommandMetaData);

        private ICommand ClosingCommand
        {
            get => (ICommand)GetValue(ClosingCommandProperty);
            set => SetValue(ClosingCommandProperty, value);
        }

        #endregion //Dependency Properties

        #region Private methods

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Closing += AssociatedObjectOnClosing;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Closing -= AssociatedObjectOnClosing;
        }

        private void OnClose()
        {
            if (_exitProcess)
                return;

            _exitProcess = true;
            _allowExit = true;

            AssociatedObject.DialogResult = DialogResultTrigger;

            _exitProcess = false;
        }

        private void AssociatedObjectOnClosing(object sender, CancelEventArgs args)
        {
            if (ClosingCommand == null || _exitProcess)
                return;

            ClosingCommand.Execute(null);

            if (!_allowExit)
                args.Cancel = true;
        }

        #endregion //Private methods
    }
}