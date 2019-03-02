using System.Collections.ObjectModel;
using System.Windows.Input;

namespace TradingClient.ViewModelInterfaces
{
    public interface IMainViewModel
    {
        #region Commands

        ICommand LoadedCommand { get; }

        ICommand ClosingCommand { get; }

        ICommand WatchListCommand { get; }

        ICommand PortfolioCommand { get; }

        ICommand AnalyzerCommand { get; }

        ICommand SignalsManagerCommand { get; }

        ICommand LogCommand { get; }

        ICommand SaveWorkspaceCommand { get; }

        ICommand LoadWorkspaceCommand { get; }

        ICommand PlaceTradeCommand { get; }

        ICommand IndividualPositionsCommand { get; }

        ICommand PendingOrdersCommand { get; }

        ICommand HistoryOrdersCommand { get; }

        ICommand CombinedPositionsCommand { get; }

        ICommand AccountInfoCommand { get; }
        
        ICommand NewIndicatorCommand { get; }

        ICommand NewSignalCommand { get; }

        ICommand EditIndicatorCommand { get; }

        ICommand EditSignalCommand { get; }

        ICommand ExitCommand { get; }

        ICommand ShowReportCommand { get; }

        ICommand SendIndicatorToServerCommand { get; }

        ICommand RemoveIndicatorFromServerCommand { get; }

        ICommand EditBrokerAccountsCommand { get; }

        IDocumentViewModel ActiveDocument { get; set; }

        ICommand DepthViewCommand { get; }

        ICommand ScriptingLogCommand { get; }

        ICommand HideNotificationCommand { get; }

        #endregion //Commands

        #region Properties

        ObservableCollection<IDocumentViewModel> Documents { get; }

        string NotificationMessage { get; }

        bool IsNotificationVisible { get; }

        bool IsUserLoggedIn { get; }

        bool IsLogOpen { get; }

        bool IsIndividualPositionsOpen { get; }

        bool IsCombinedPositionsOpen { get; }

        bool IsHistoryOrdersOpen { get; }

        bool IsAccountInfoOpened { get; }

        bool IsPendingOrdersOpen { get; }

        bool IsDepthViewOpen { get; }

        bool IsTradingAllowed { get; }

        bool IsPortfolioOpen { get; }

        bool IsAnalyzerOpen { get; }

        bool IsSignalsMgrOpen { get; }

        #endregion //Properties

        #region Methods

        void ShowNotification(string message, byte displayDuration = 0);

        void ShowAlertsPopup(string scriptingId);

        void ShowOutputPopup(string scriptingId);

        void ShowSignalParamSpace(string signalName);
        
        void ShowBacktestSettings(string signalName);

        void ShowBacktestResults(string signalName);

        #endregion //Methods
    }
}