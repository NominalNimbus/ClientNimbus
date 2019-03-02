using System.Collections.ObjectModel;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModelInterfaces
{
    public interface IDepthViewItem
    {
        ObservableCollection<DepthViewRecord> Records { get; set; }
        string Broker { get; set; }
        string DataFeed { get; set; }
        string Symbol { get; set; }
    }
}
