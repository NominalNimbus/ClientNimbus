using System.Collections.ObjectModel;
using System.Windows.Input;
using TradingClient.Data.Contracts;

namespace TradingClient.ViewModelInterfaces
{
    public interface ISignalsManagerViewModel : IDocumentViewModel
    {
        ObservableCollection<Signal> Signals { get; }
        ICommand StartCommand { get; }
        ICommand PauseCommand { get; }
        ICommand StopCommand { get; }
        ICommand ShowBacktestSettingsCommand { get; }
        ICommand ShowParamSpaceCommand { get; }
        ICommand ShowBacktestResultsCommand { get; }
        ICommand ShowOutputCommand { get; }
        ICommand ShowAlertsCommand { get; }
    }
}