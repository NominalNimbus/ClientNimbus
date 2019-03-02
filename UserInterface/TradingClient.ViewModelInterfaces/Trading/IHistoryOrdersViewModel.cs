using System.Windows.Input;

namespace TradingClient.ViewModelInterfaces
{
    public interface IHistoryOrdersViewModel : IOrdersBaseViewModel
    {
        bool CanRequestMoreOrders { get; }

        ICommand ShowMoreOrdersCommand { get; }

        ICommand ClearCommand { get; }
    }
}