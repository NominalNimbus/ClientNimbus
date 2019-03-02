using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public class SignalsManagerViewModel : DocumentViewModel, ISignalsManagerViewModel
    {
        private readonly IMainViewModel _mainView;

        public ICommand StartCommand { get; private set; }
        public ICommand PauseCommand { get; private set; }
        public ICommand StopCommand { get; private set; }
        public ICommand ShowBacktestSettingsCommand { get; private set; }
        public ICommand ShowParamSpaceCommand { get; private set; }
        public ICommand ShowBacktestResultsCommand { get; private set; }
        public ICommand ShowOutputCommand { get; private set; }
        public ICommand ShowAlertsCommand { get; private set; }

        private IApplicationCore Core { get; }

        public override string Title => "Signals Manager";

        public override DocumentType DocumentType => DocumentType.SignalsManager;

        public ObservableCollection<Signal> Signals
        {
            get
            {
                return new ObservableCollection<Signal>(Core.DataManager.Portfolios
                    .SelectMany(i => i.Strategies).SelectMany(i => i.Signals));
            }
        }

        public SignalsManagerViewModel(IApplicationCore core, IMainViewModel mainView)
        {
            Core = core;
            _mainView = mainView;

            StartCommand = new RelayCommand<Signal>(StartSignal);
            PauseCommand = new RelayCommand<Signal>(PauseSignal);
            StopCommand = new RelayCommand<Signal>(StopSignal);
            ShowBacktestSettingsCommand = new RelayCommand<Signal>(ShowBacktestSettings);
            ShowParamSpaceCommand = new RelayCommand<Signal>(ShowParamSpace);
            ShowBacktestResultsCommand = new RelayCommand<Signal>(ShowBacktestResultsView);
            ShowOutputCommand = new RelayCommand<Signal>(s => _mainView?.ShowOutputPopup(s.FullName));
            ShowAlertsCommand = new RelayCommand<Signal>(s => _mainView?.ShowAlertsPopup(s.FullName));

            Core.DataManager.OnPortfolioChanged += (s, e) => OnPropertyChanged("Signals");
            Core.DataManager.ScriptingManager.SignalInstanceUpdated += (s, e) => RefreshSignal(e.Value);
        }

        private void RefreshSignal(Signal signal)
        {
            var item = Signals.FirstOrDefault(i => i.FullName == signal.FullName);
            if (item != null)
            {
                item.State = signal.State;
                item.Parameters = signal.Parameters.ToList();
                item.Selections = signal.Selections.ToList();
                item.BacktestProgress = signal.BacktestProgress;
            }
        }

        private void StartSignal(Signal signal)
        {
            if (signal == null || !signal.IsDeployed)
                return;

            if (signal.State == State.Stopped)
            {
                if (!signal.Selections.Any() || !signal.Parameters.Any())
                {
                    Core.ViewFactory.ShowMessage("Please set instruments and parameters for this signal");
                    return;
                }

                if (signal.IsInBacktestMode)
                {
                    if (signal.NumericParamsCount == 0)
                    {
                        Core.ViewFactory.ShowMessage("This signal doesn't have any parameters to be backtested",
                            "Invalid parameters space", MsgBoxButton.OK, MsgBoxIcon.Error);
                        return;
                    }

                    if (signal.BacktestSettings == null || signal.Parent.BacktestSettings == null)
                    {
                        Core.ViewFactory.ShowMessage("Please define signal backtest settings first");
                        return;
                    }

                    if (signal.IsDefaultParamSpaceUsed)
                    {
                        bool? shouldUseDefaults = Core.Settings.UseDefaultSignalBacktestSettings;
                        if (shouldUseDefaults != true)
                        {
                            using (var dlg = new CheckableDialogViewModel("Some parameter space values are using default values. "
                                + "\r\nWould you like to use these values for backtest?", "Question"))
                            {
                                shouldUseDefaults = Core.ViewFactory.ShowDialogView(dlg);
                                if (shouldUseDefaults == true && dlg.IsChecked)
                                    Core.Settings.UseDefaultSignalBacktestSettings = shouldUseDefaults;
                            }
                        }

                        if (shouldUseDefaults != true)
                            return;
                    }

                    var paramSpaceError = signal.ValidateParamSpace();
                    if (paramSpaceError != null)
                    {
                        Core.ViewFactory.ShowMessage(paramSpaceError, "Invalid parameter space",
                            MsgBoxButton.OK, MsgBoxIcon.Error);
                    }
                    else
                    {
                        signal.BacktestResults.Clear();
                        Core.DataManager.ScriptingManager.AddSignal(new SignalReqParams
                        {
                            FullName = signal.FullName,
                            StrategyParameters = new StrategyParams(signal.Parent),
                            IsSimulated = false,
                            Parameters = signal.Parameters,
                            Selections = signal.Selections,
                            BacktestSettings = signal.BacktestSettings,
                            StrategyBacktestSettings = signal.Parent.BacktestSettings,
                            Accounts = signal.Parent.Parent.Accounts.ToList()
                        },
                        Core.PathManager.GetDirectory4Signal(Core.Settings.UserName, signal.FullName),
                        GetSignalSolutionDir(signal.Name, Core.PathManager.SignalsDirectory));
                    }
                }
                else  //not in backtest mode
                {
                    Core.DataManager.ScriptingManager.AddSignal(new SignalReqParams
                    {
                        FullName = signal.FullName,
                        StrategyParameters = new StrategyParams(signal.Parent),
                        IsSimulated = false,
                        Parameters = signal.Parameters,
                        Selections = signal.Selections,
                        Accounts = signal.Parent.Parent.Accounts.ToList()
                    },
                    Core.PathManager.GetDirectory4Signal(Core.Settings.UserName, signal.FullName),
                    GetSignalSolutionDir(signal.Name, Core.PathManager.SignalsDirectory));
                }
            }
            else if (signal.State == State.Paused)
            {
                Core.DataManager.ScriptingManager.InvokeSignalAction(signal.FullName, SignalAction.SetSimulatedOff);
            }
            else if (signal.State == State.BacktestPaused)
            {
                Core.DataManager.ScriptingManager.InvokeSignalAction(signal.FullName, SignalAction.ResumeBacktest);
            }
        }

        private void PauseSignal(Signal signal)
        {
            if (signal == null || !signal.IsDeployed)
                return;

            if (signal.State == State.Working)
                Core.DataManager.ScriptingManager.InvokeSignalAction(signal.FullName, SignalAction.SetSimulatedOn);
            else if (signal.State == State.Backtesting)
                Core.DataManager.ScriptingManager.InvokeSignalAction(signal.FullName, SignalAction.PauseBacktest);
        }

        private void StopSignal(Signal signal)
        {
            if (signal != null && signal.State != State.New && signal.State != State.Stopped)
            {
                Core.DataManager.ScriptingManager.RemoveSignal(signal.FullName);
                signal.State = State.Stopped;
            }
        }

        private void ShowBacktestSettings(Signal signal)
        {
            if (signal != null)
                _mainView.ShowBacktestSettings(signal.FullName);
        }

        private void ShowParamSpace(Signal signal)
        {
            if (signal != null)
                _mainView.ShowSignalParamSpace(signal.FullName);
        }

        private void ShowBacktestResultsView(Signal signal)
        {
            if (signal != null)
                _mainView.ShowBacktestResults(signal.FullName);
        }

        private void ShowOutputView()
        {
            throw new NotImplementedException();
        }

        private static string GetSignalSolutionDir(string name, string signalsDir)
        {
            var slnFiles = Directory.GetFiles(signalsDir, name + ".sln", SearchOption.AllDirectories);
            return slnFiles.Any() ? Path.GetDirectoryName(slnFiles[0]) : null;
        }
    }
}