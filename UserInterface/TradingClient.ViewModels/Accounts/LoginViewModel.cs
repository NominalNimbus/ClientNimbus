using System;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight.CommandWpf;
using TradingClient.Interfaces;

namespace TradingClient.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        #region Members
                
        private bool _isWait;

        #endregion // Members
        
        #region Constructors

        public LoginViewModel(IApplicationCore core)
        {
            Core = core;
            Host = core.Settings.HostAddress;
            Port = core.Settings.Port;
            UserName = core.Settings.UserName;
            LoginCommand = new RelayCommand(CancelCommandExecute, () => !_isWait);
            CancelCommand = new RelayCommand(() => DialogResult = false);
        }

        #endregion // Constructors

        #region Properties

        private IApplicationCore Core { get; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        #endregion // Properties

        #region Commands

        public ICommand LoginCommand { get; private set; }

        public ICommand CancelCommand { get; private set; }

        #endregion //Commands

        #region Private methods

        private string Validate()
        {
            if (string.IsNullOrEmpty(UserName))
            {
                return "Login name is empty";
            }
            else if (string.IsNullOrEmpty(Password))
            {
                return "Password is empty";
            }
            else if (string.IsNullOrEmpty(Host))
            {
                return "Host is empty";
            }
            return string.Empty;
        }

        private void CancelCommandExecute()
        {
            var error = Validate();
            if (!string.IsNullOrEmpty(error))
            {
                Core.ViewFactory.ShowMessage(error);
                return;
            }

            Core.Settings.UserName = UserName;
            Core.Settings.HostAddress = Host;
            Core.Settings.Port = Port;

            _isWait = true;
            Task.Factory.StartNew(Connect);
        }

        private void Connect()
        {
            string failure = null;
            try
            {
                Core.DataManager.Login(Host, Port, UserName, Password, error =>
                {
                    _isWait = false;
                    failure = error;
                    if (string.IsNullOrWhiteSpace(error))
                        DialogResult = true;
                });
            }
            catch (Exception e)
            {
                _isWait = false;
                failure = e.Message;
            }

            if (!string.IsNullOrWhiteSpace(failure))
            {
                failure += (Environment.NewLine + Environment.NewLine + "Continue without login?");
                var result = Core.ViewFactory.ShowMessage(failure, "Login Failed", MsgBoxButton.YesNo, MsgBoxIcon.Warning);
                if (result == DlgResult.Yes)
                    DialogResult = true;
            }
        }

        #endregion //Private methods
    }
}