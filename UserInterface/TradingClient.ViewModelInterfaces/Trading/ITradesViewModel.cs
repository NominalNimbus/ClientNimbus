using System.Collections.ObjectModel;
using System.Windows.Input;
using TradingClient.Data.Contracts;

namespace TradingClient.ViewModelInterfaces
{
    public interface ITradesViewModel : IDocumentViewModel
    {
        ObservableCollection<AccountInfo> Accounts { get; }

        AccountInfo SelectedAccount { get; set; }

        bool IsTradingAllowed { get; }

        ICommand ExportCommand { get; }

    }
}