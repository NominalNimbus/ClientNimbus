using System.Collections.ObjectModel;
using System.Windows.Input;
using TradingClient.Data.Contracts;

namespace TradingClient.ViewModelInterfaces
{
    public interface IPortfolioViewModel : IDocumentViewModel
    {
        ObservableCollection<IOrderItem> Orders { get; }
        ObservableCollection<IOrderItem> PendingOrders { get; }
        ObservableCollection<IOrderItem> HistoricalOrders { get; }
        ObservableCollection<IPositionItem> Positions { get; }
        ObservableCollection<AccountWithName> Accounts { get; }
        ObservableCollection<Portfolio> Portfolios { get; }

        AccountInfo SelectedAccount { get; set; }
        Portfolio SelectedPortfolio { get; set; }
        IOrderItem SelectedOrder { get; set; }
        IOrderItem SelectedPendingOrder { get; set; }
        IPositionItem SelectedPosition { get; set; }

        decimal OverallBalance { get; }
        decimal OverallMargin { get; }
        decimal OverallEquity { get; }
        decimal OverallProfit { get; }

        ICommand PlaceOpposite { get; }
        ICommand ClosePosition { get; }
        ICommand ModifyTradeCommand { get; }
        ICommand ModifyPendingCommand { get; }
        ICommand CancelPendingCommand { get; }
        ICommand CreatePortfolioCommand { get; }
        ICommand EditPortfolioCommand { get; }
        ICommand RemovePortfolioCommand { get; }
        ICommand ClearCommand { get; }
        bool IsTradingAllowed { get; }
    }

    public class AccountWithName
    {
        public AccountInfo Account { get; set; }
        public string Name { get; set; }
    }
}