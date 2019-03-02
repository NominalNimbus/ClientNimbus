using System.Collections.ObjectModel;
using System.Windows.Input;

namespace TradingClient.ViewModelInterfaces
{
    public interface IPendingOrdersViewModel : IOrdersBaseViewModel
    {
        ICommand DeleteTradeCommand { get; }
    }
}