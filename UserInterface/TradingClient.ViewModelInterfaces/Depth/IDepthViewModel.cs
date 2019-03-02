using System.Collections.ObjectModel;
using System.Windows.Input;

namespace TradingClient.ViewModelInterfaces
{
    public interface IDepthViewModel : IDocumentViewModel
    {
        ObservableCollection<IDepthViewItem> Items { get; set; }
        int SelectedIndex { get; set; }
        ICommand CloseDepthTabCommand { get; }
        void AddNewItem(string symbol, string dataFeed);
    }
}