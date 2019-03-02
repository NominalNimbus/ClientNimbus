using System.Collections.ObjectModel;
using TradingClient.Data.Contracts;

namespace TradingClient.ViewModelInterfaces
{
    public interface IAccountsViewModel
    {
       ObservableCollection<AccountInfo> Accounts { get; } 
    }
}