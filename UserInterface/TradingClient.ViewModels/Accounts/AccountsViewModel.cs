using System.Collections.ObjectModel;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public class AccountsViewModel : DocumentViewModel, IAccountsViewModel
    {
        public AccountsViewModel(IApplicationCore core)
        {
            Accounts = core.DataManager.Broker.ActiveAccounts;
        }

        public override string Title => "Accounts information";

        public override DocumentType DocumentType => DocumentType.AccountInfo;

        public ObservableCollection<AccountInfo> Accounts { get; private set; }

    }
}
