using System;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using TradingClient.Common;
using TradingClient.Interfaces;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public class ReconnectViewModel : ViewModelBase
    {
        #region Members
                
        private string _error;
        private bool _allowReconnect;
        private bool _isReconnecting;

        #endregion //Members

        public ReconnectViewModel(IApplicationCore core, string reason, bool allowReconnect = true)
        {
            Core = core;
            Message = reason;
            AllowReconnect = allowReconnect;

            OkCommand = new RelayCommand(OkCommandExecution, () => AllowReconnect);
            CancelCommand = new RelayCommand(() => DialogResult = false);
        }

        #region Properties

        private IApplicationCore Core { get; }

        public bool AllowReconnect
        {
            get => _allowReconnect && !_isReconnecting;
            set => SetPropertyValue(ref _allowReconnect, value, nameof(AllowReconnect));
        }

        public bool IsReconnecting
        {
            get => _isReconnecting;
            set => SetPropertyValue(ref _isReconnecting, value, nameof(IsReconnecting));
        }

        public string Message { get; set; }

        public string Error
        {
            get => _error;
            set => SetPropertyValue(ref _error, value, nameof(Error));
        }

        #endregion //Properties

        #region Commands

        public ICommand OkCommand { get; private set; }

        public ICommand CancelCommand { get; private set; }

        private void OkCommandExecution()
        {
            if (IsReconnecting)
                return;

            IsReconnecting = true;
            Task.Factory.StartNew(Reconnect);
        }

        #endregion //Commands

        #region Helper methods

        private void Reconnect()
        {
            try
            {
                Error = Core.DataManager.Reconnect();
                Core.DataManager.Broker.Reconnect();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }

            IsReconnecting = false;

            if (string.IsNullOrEmpty(Error))
                DialogResult = true;
            else
                Core.ViewFactory.ShowMessage(Error);
        }

        #endregion Helper methods

    }
}