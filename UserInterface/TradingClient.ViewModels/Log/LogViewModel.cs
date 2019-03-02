using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using TradingClient.Interfaces;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public class LogViewModel : DocumentViewModel, ILogViewModel
    {
        #region Properties

        private IApplicationCore Core { get; }

        public override string Title => "Logs";

        public override DocumentType DocumentType => DocumentType.Log;

        public ObservableCollection<ILogItem> Items { get; private set; }

        public ICommand ClearLogsCommand { get; private set; }

        #endregion

        public LogViewModel(IApplicationCore core)
        {
            Core = core;
            Items = new ObservableCollection<ILogItem>(LogManager.Items.ToList());

            LogManager.OnNew += LogManager_OnNew;
            ClearLogsCommand = new RelayCommand(() => Items.Clear(), () => Items.Count > 0);
        }

        private void LogManager_OnNew(LogItem msg)
        {
            Core.ViewFactory.BeginInvoke(() => Items.Insert(0, msg));
        }
    }
}