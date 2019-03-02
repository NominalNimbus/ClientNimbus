using System.Windows;
using System.Windows.Controls;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.BaseStyles.Selectors
{
    public class DocumentTemplateSelector : DataTemplateSelector
    {
        #region Properties

        public DataTemplate WatchListTemplate { get; set; }

        public DataTemplate LogTemplate { get; set; }

        public DataTemplate IndividualPositionsTemplate { get; set; }

        public DataTemplate HistoryOrdersTemplate { get; set; }

        public DataTemplate PendingOrdersTemplate { get; set; }

        public DataTemplate CombinedPositionsTemplate { get; set; }

        public DataTemplate ScriptingLogTemplate { get; set; }

        public DataTemplate AccountsTemplate { get; set; }

        public DataTemplate PortfolioTemplate { get; set; }

        public DataTemplate AnalyzerTemplate { get; set; }

        public DataTemplate SignalsManagerTemplate { get; set; }

        public DataTemplate DepthTemplate { get; set; }

        #endregion //Properties

        #region Overrides

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            switch (item)
            {
                case ILogViewModel _:
                    return LogTemplate;
                case IWatchListViewModel _:
                    return WatchListTemplate;
                case IIndividualPositionsViewModel _:
                    return IndividualPositionsTemplate;
                case IHistoryOrdersViewModel _:
                    return HistoryOrdersTemplate;
                case IAccountsViewModel _:
                    return AccountsTemplate;
                case ICombinedPositionsViewModel _:
                    return CombinedPositionsTemplate;
                case IScriptingLogViewModel _:
                    return ScriptingLogTemplate;
                case IPendingOrdersViewModel _:
                    return PendingOrdersTemplate;
                case IPortfolioViewModel _:
                    return PortfolioTemplate;
                case IAnalyzerViewModel _:
                    return AnalyzerTemplate;
                case ISignalsManagerViewModel _:
                    return SignalsManagerTemplate;
                case IDepthViewModel _:
                    return DepthTemplate;
                default:
                    return base.SelectTemplate(item, container);
            }
        }

        #endregion //Overrides
    }
}