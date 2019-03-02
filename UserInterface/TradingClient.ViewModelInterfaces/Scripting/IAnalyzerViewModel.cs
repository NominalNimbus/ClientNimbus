using System.Collections.ObjectModel;
using System.Windows.Input;
using TradingClient.Data.Contracts;

namespace TradingClient.ViewModelInterfaces
{
    public interface IAnalyzerViewModel : IDocumentViewModel
    {
        ObservableCollection<Portfolio> Portfolios { get; }

        object SelectedItem { get; set; }
        Portfolio SelectedPortfolio { get; }
        Strategy SelectedStrategy { get; }
        Signal SelectedSignal { get; }

        bool IsPortfolioSelected { get; }
        bool IsStrategySelected { get; }
        bool IsSignalSelected { get; }
        bool IsSelectedSignalStartable { get; }
        bool IsSelectedSignalPausable { get; }
        bool IsSelectedSignalStoppable { get; }

        ICommand CreatePortfolioCommand { get; }
        ICommand CreateSignalCommand { get; }
        ICommand DeleteSelectedItemCommand { get; }
        ICommand EditPortfolioCommand { get; }
        ICommand ClonePortfolioCommand { get; }
        ICommand AddStrategyCommand { get; }
        ICommand EditStrategyCommand { get; }
        ICommand AddSignalCommand { get; }
        ICommand EditStrategyInstrumentsCommand { get; }
        ICommand RunStrategySignalsCommand { get; }
        ICommand StopStrategySignalsCommand { get; }
        ICommand ClearStrategySignalsCommand { get; }
        ICommand DeploySignalCommand { get; }
        ICommand StartSignalCommand { get; }
        ICommand PauseSignalCommand { get; }
        ICommand DeployCommand { get; }
        ICommand StopSignalCommand { get; }
        ICommand StartSignalBacktestCommand { get; }
        ICommand PauseOrResumeSignalBacktestCommand { get; }

        ICommand EditInstrumentsCommand { get; }
        ICommand UpdateInstrumentsCommand { get; }
        ICommand AddAllFeedSymbolsCommand { get; }
        ICommand ShowSignalParamSpaceCommand { get; }
        ICommand ShowChartWithTradesCommand { get; }
        ICommand SaveStrategyBacktestSettingsCommand { get; }
        ICommand SaveSignalBacktestSettingsCommand { get; }
        ICommand LoadSignalBacktestSettingsCommand { get; }
        ICommand SaveBacktestResultsCommand { get; }
        ICommand ExportBacktestResultsToCSVCommand { get; }
        ICommand LoadBacktestResultsCommand { get; }
        ICommand LocateBarDataDirectoryCommand { get; }

        void ShowSignalParamSpace(string signalName);
        void ShowBacktestSettings(string signalName);
        void ShowBacktestResults(string signalName);
    }
}