using System.Collections.ObjectModel;
using System.Windows.Input;

namespace TradingClient.ViewModelInterfaces
{
    public interface ILogViewModel : IDocumentViewModel
    {
        ObservableCollection<ILogItem> Items { get; }

        ICommand ClearLogsCommand { get; }
    }
}