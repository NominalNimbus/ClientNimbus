using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TradingClient.Common;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;

namespace TradingClient.ViewModels
{
    public class CreateSimulatedAccountViewModel : ViewModelBase
    {

        #region Members

        private readonly AvailableBrokerInfo _brokerInfo;

        private IApplicationCore Core { get; }

        public CreateSimulatedBrokerAccountInfoItem Account { get; set; }

        #endregion //Members

        #region Commands

        public ICommand CreateCommand { get; private set; }

        public ICommand CancelCommand { get; private set; }

        #endregion //Commands

        #region Constructor

        public CreateSimulatedAccountViewModel(IApplicationCore core, AvailableBrokerInfo brokerInfo)
        {
            Core = core;
            _brokerInfo = brokerInfo;
            Account = new CreateSimulatedBrokerAccountInfoItem(brokerInfo.BrokerName)
            {
                Currencies = new ObservableCollection<string>(Core.DataManager.Broker.AvailableCurrencies)
            };

            CreateCommand = new RelayCommand(CreateCommandExecute);
            CancelCommand = new RelayCommand(CancelCommandExecute);
        }

        #endregion //Constructor

        #region Command executions

        private void CreateCommandExecute()
        {
            var error = ValidateAccount();
            if(!string.IsNullOrEmpty(error))
            {
                Core.ViewFactory.ShowMessage(error, "Error", MsgBoxButton.OK, MsgBoxIcon.Warning);
                return;
            }

            DialogResult = true;
        }

        private void CancelCommandExecute() =>
            DialogResult = false;

        #endregion //Command executions

        #region Helper methods

        private string ValidateAccount()
        {
            if(string.IsNullOrEmpty(Account.AccountName))
            {
                return "Please set account name";
            }
            if(_brokerInfo.Accounts.Contains(Account.AccountName))
            {
                return "Account with this name already exst";
            }
            if(string.IsNullOrEmpty(Account.Currency))
            {
                return "Please set currency";
            }
            if(Account.Ballance <= 0)
            {
                return "Please set ballance";
            }

            return string.Empty;
        }

        #endregion //Helper methods
    }


}
