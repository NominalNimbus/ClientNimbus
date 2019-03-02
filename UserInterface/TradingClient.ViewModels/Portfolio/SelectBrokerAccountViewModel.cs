using System.Collections.ObjectModel;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;

namespace TradingClient.ViewModels
{
    public class SelectBrokerAccountViewModel : ViewModelBase
    {
        private IApplicationCore Core { get; }

        public ObservableCollection<AccountInfo> Accounts { get; private set; }

        public AccountInfo SelectedAccount { get; set; }

        public ICommand OkCommand { get; private set; }

        public ICommand CancelCommand { get; private set; }

        public SelectBrokerAccountViewModel(IApplicationCore core)
        {
            Core = core;

            Accounts = new ObservableCollection<AccountInfo>(Core.DataManager.Broker.ActiveAccounts);
            if (Accounts.Count > 0)
                SelectedAccount = Accounts[0];

            OkCommand = new RelayCommand(() =>
            {
                DialogResult = true;
            }, () => SelectedAccount != null);

            CancelCommand = new RelayCommand(() =>
            {
                DialogResult = false;
            });
        }
    }
}
