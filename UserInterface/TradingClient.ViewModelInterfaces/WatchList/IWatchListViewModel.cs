using System.Collections.ObjectModel;
using System.Windows.Input;
using GalaSoft.MvvmLight.CommandWpf;

namespace TradingClient.ViewModelInterfaces
{
    public interface IWatchListViewModel : IDocumentViewModel
    {
        #region Properties

        ObservableCollection<string> DataFeeds { get; }

        ObservableCollection<IWatchItem> Items { get; }

        IWatchItem SelectedItem { get; set; }

        bool DateCol { get; set; }

        bool PriceCol { get; set; }

        bool BidCol { get; set; }

        bool BidSizeCol { get; set; }

        bool AskCol { get; set; }

        bool AskSizeCol { get; set; }

        bool OpenCol { get; set; }

        bool HighCol { get; set; }

        bool LowCol { get; set; }

        #endregion //Properties

        #region Commands

        ICommand PlaceOrderCommand { get; }

        ICommand AddToDepthViewCommand { get; }

        ICommand RemoveSelectedCommand { get; }

        RelayCommand<IWatchItem> RemoveCurrentCommand { get; }

        ICommand ExportCommand { get; }

        ICommand ImportCommand { get; }

        #endregion //Commands

    }
}