using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;
using TradingClient.ViewModelInterfaces;
using SignalState = TradingClient.Data.Contracts.State;

namespace TradingClient.ViewModels
{
    public class AnalyzerViewModel : DocumentViewModel, IAnalyzerViewModel
    {
        #region Members, Commands and Properties
        private readonly IScriptingManager _scriptingManager;
        private readonly IMainViewModel _mainView;
        private readonly List<string> _signalsToDeploy;
        private readonly string _userName;
        private object _selectedItem;
        private int _selectedTab;
        private string _selectedItemKey;

        private IApplicationCore Core { get; }

        public override string Title => "Analyzer";

        public override DocumentType DocumentType => DocumentType.Analyzer;

        private string UserFolder => Path.Combine(Core.PathManager.PortfolioDirectory, Core.Settings.UserName);

        public ObservableCollection<Portfolio> Portfolios { get; private set; }

        public ObservableCollection<DataGridColumn> BacktestResultColumns
        {
            get
            {
                if (SelectedSignal == null || !SelectedSignal.BacktestResults.Any())
                    return new ObservableCollection<DataGridColumn>();

                var ret = new ObservableCollection<DataGridColumn>
                {
                    new DataGridTextColumn {MinWidth = 120, Width = 200, Binding = new Binding("Title")},
                    CreateColumn("Trades", 50, "NumberOfTrades"),
                    CreateColumn("Positions", 64, "TotalNumberOfPositions"),
                    CreateColumn("Profitable Positions", 68, "NumberOfProfitablePositions"),
                    CreateColumn("Losing Positions", 60, "NumberOfLosingPositions"),
                    CreateColumnWithFormat("Total Profit", 52, "TotalProfit"),
                    CreateColumnWithFormat("Total Loss", 46, "TotalLoss"),
                    CreateColumnWithFormat("% Profit", 46, "PercentProfit"),
                    CreateColumnWithFormat("Largest Profit", 54, "LargestProfit"),
                    CreateColumnWithFormat("Largest Loss", 54, "LargestLoss"),
                    CreateColumnWithFormat("Max Draw Down", 70, "MaximumDrawDown"),
                    CreateColumnWithFormat("Max Draw Down Monte Carlo", 105, "MaximumDrawDownMonteCarlo"),
                    CreateColumnWithFormat("Compound Monthly ROR", 85, "CompoundMonthlyROR"),
                    CreateColumnWithFormat("Standard Deviation", 70, "StandardDeviation"),
                    CreateColumnWithFormat("Std Dev Annualized", 75, "StandardDeviationAnnualized"),
                    CreateColumnWithFormat("Downside Dev Mar10", 80, "DownsideDeviationMar10"),
                    CreateColumnWithFormat("Value Added Monthly Idx", 85, "ValueAddedMonthlyIndex"),
                    CreateColumnWithFormat("Sharpe Ratio", 54, "SharpeRatio"),
                    CreateColumnWithFormat("Sortino Ratio MAR5", 82, "SortinoRatioMAR5"),
                    CreateColumnWithFormat("Annualized Sortino Ratio", 84, "AnnualizedSortinoRatioMAR5"),
                    CreateColumnWithFormat("Sterling Ratio MAR5", 75, "SterlingRatioMAR5"),
                    CreateColumnWithFormat("Calmar Ratio", 55, "CalmarRatio"),
                    CreateColumnWithFormat("Risk Reward Ratio", 75, "RiskRewardRatio")
                };

                /*var button = new FrameworkElementFactory(typeof(Button));
                button.SetValue(ContentControl.ContentProperty, "Chart");
                button.SetValue(FrameworkElement.MarginProperty, new Thickness(0));
                button.SetValue(ButtonBase.CommandProperty, new Binding("DataContext.ShowChartWithTradesCommand") { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1) });
                button.SetValue(ButtonBase.CommandParameterProperty, new Binding());
                button.SetValue(UIElement.VisibilityProperty, new Binding("IsNotAggregated") { Converter = new BoolToVisibilityConverter() });
                ret.Add(new DataGridTemplateColumn { Width = 60, MinWidth = 60, CellTemplate = new DataTemplate { VisualTree = button } });*/

                if (SelectedSignal.BacktestResults.Any() && SelectedSignal.BacktestResults[0].Parameters.Any())
                {
                    foreach (var p in SelectedSignal.BacktestResults[0].Parameters)
                    {
                        ret.Add(new DataGridTextColumn
                        {
                            Header = p.Key,
                            MinWidth = 50,
                            Width = 70,
                            Binding = new Binding($"Parameters[{p.Key.Replace(" ", "^ ")}]") { StringFormat = "0.##" }
                        });
                    }
                }
                return ret;
            }
        }

        public object SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChanged("SelectedItem");
                    OnPropertyChanged("SelectedPortfolio");
                    OnPropertyChanged("SelectedStrategy");
                    OnPropertyChanged("IsPortfolioSelected");
                    OnPropertyChanged("IsStrategySelected");
                    RefreshSignalGui();

                    UpdateInstruments();
                    GetRecentBacktestResults();

                    SelectedTabIndex = GetSelectedTabIndexByItemType(value);
                }
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTab;
            set
            {
                if (value > -1 && _selectedTab != value)
                {
                    _selectedTab = value;
                    OnPropertyChanged("SelectedTabIndex");

                    //SelectClosestItemByTabIndex();
                }
            }
        }

        public Portfolio SelectedPortfolio => _selectedItem as Portfolio;

        public Strategy SelectedStrategy => _selectedItem as Strategy;

        public Signal SelectedSignal => _selectedItem as Signal;

        public bool IsPortfolioSelected => _selectedItem is Portfolio;

        public bool IsStrategySelected => _selectedItem is Strategy;

        public bool IsSignalSelected => _selectedItem is Signal;

        public bool IsSelectedSignalStartable => _selectedItem is Signal
                                                 && (SelectedSignal.State == SignalState.Stopped
                                                     || SelectedSignal.State == SignalState.Paused);

        public bool IsSelectedSignalPausable => _selectedItem is Signal
                                                && SelectedSignal.State == SignalState.Working;

        public bool IsSelectedSignalStoppable => _selectedItem is Signal
                                                 && SelectedSignal.State != SignalState.New
                                                 && SelectedSignal.State != SignalState.Stopped;

        public ICommand DeployCommand { get; private set; }

        public ICommand CreatePortfolioCommand { get; private set; }

        public ICommand CreateSignalCommand { get; private set; }

        public ICommand DeleteSelectedItemCommand { get; private set; }

        public ICommand EditPortfolioCommand { get; private set; }

        public ICommand ClonePortfolioCommand { get; private set; }

        public ICommand AddStrategyCommand { get; private set; }

        public ICommand EditStrategyCommand { get; private set; }

        public ICommand AddSignalCommand { get; private set; }

        public ICommand EditStrategyInstrumentsCommand { get; private set; }

        public ICommand RunStrategySignalsCommand { get; private set; }

        public ICommand StopStrategySignalsCommand { get; private set; }

        public ICommand ClearStrategySignalsCommand { get; private set; }

        public ICommand DeploySignalCommand { get; private set; }

        public ICommand StartSignalCommand { get; private set; }

        public ICommand PauseSignalCommand { get; private set; }

        public ICommand StopSignalCommand { get; private set; }

        public ICommand StartSignalBacktestCommand { get; private set; }

        public ICommand PauseOrResumeSignalBacktestCommand { get; private set; }

        public ICommand SaveStrategyBacktestSettingsCommand { get; private set; }

        public ICommand SaveSignalBacktestSettingsCommand { get; private set; }

        public ICommand LoadSignalBacktestSettingsCommand { get; private set; }

        public ICommand EditInstrumentsCommand { get; private set; }

        public ICommand UpdateInstrumentsCommand { get; private set; }

        public ICommand AddAllFeedSymbolsCommand { get; private set; }

        public ICommand ShowSignalParamSpaceCommand { get; private set; }

        public ICommand ShowChartWithTradesCommand { get; private set; }

        public ICommand SaveBacktestResultsCommand { get; private set; }

        public ICommand ExportBacktestResultsToCSVCommand { get; private set; }

        public ICommand LoadBacktestResultsCommand { get; private set; }

        public ICommand LocateBarDataDirectoryCommand { get; private set; }
        #endregion

        public AnalyzerViewModel(IApplicationCore core, IMainViewModel mainView)
        {
            Core = core;
            _scriptingManager = core.DataManager.ScriptingManager;
            _mainView = mainView;
            _signalsToDeploy = new List<string>();
            _userName = Core.Settings.UserName;

            Portfolios = Core.DataManager.Portfolios;
            SelectedItem = Portfolios.Count > 0 ? Portfolios[0] : null;
            RefreshAllSignals();

            DeployCommand = new RelayCommand(DeployStrategy);

            CreatePortfolioCommand = new RelayCommand(CreatePortfolio);
            CreateSignalCommand = new RelayCommand(CreateSignal);
            DeleteSelectedItemCommand = new RelayCommand(DeleteSelectedItem);
            EditPortfolioCommand = new RelayCommand(EditPortfolio);
            ClonePortfolioCommand = new RelayCommand(ClonePortfolio);
            AddStrategyCommand = new RelayCommand(AddStrategy);
            EditStrategyCommand = new RelayCommand(EditStrategy);
            AddSignalCommand = new RelayCommand(AddSignalToStrategy);
            EditStrategyInstrumentsCommand = new RelayCommand(EditStrategyInstruments);
            RunStrategySignalsCommand = new RelayCommand(RunStrategySignals);
            StopStrategySignalsCommand = new RelayCommand(StopStrategySignals);
            ClearStrategySignalsCommand = new RelayCommand(ClearStrategySignals);
            DeploySignalCommand = new RelayCommand(() => DeploySignal(SelectedSignal));
            StartSignalCommand = new RelayCommand(StartSignalExecution);
            PauseSignalCommand = new RelayCommand(PauseSignalExecution);
            StopSignalCommand = new RelayCommand(StopSignalExecution);
            StartSignalBacktestCommand = new RelayCommand(StartSignalBacktest);
            PauseOrResumeSignalBacktestCommand = new RelayCommand(PauseOrResumeSignalBacktest);
            SaveStrategyBacktestSettingsCommand = new RelayCommand<Strategy>(SaveStrategyBacktestSettings);
            SaveSignalBacktestSettingsCommand = new RelayCommand<Signal>(SaveSignalBacktestSettings);
            LoadSignalBacktestSettingsCommand = new RelayCommand<Signal>(LoadSignalBacktestSettings);
            LocateBarDataDirectoryCommand = new RelayCommand<Signal>(LocateBarDataDirectory);
            EditInstrumentsCommand = new RelayCommand(EditInstruments);
            UpdateInstrumentsCommand = new RelayCommand(UpdateInstruments);
            AddAllFeedSymbolsCommand = new RelayCommand<string>(AddAllSymbolsToSignal);
            ShowSignalParamSpaceCommand = new RelayCommand<string>(ShowSignalParamSpace);
            SaveBacktestResultsCommand = new RelayCommand(SaveBacktestResults);
            ExportBacktestResultsToCSVCommand = new RelayCommand(ExportBacktestResults);
            LoadBacktestResultsCommand = new RelayCommand(LoadBacktestResults);

            Core.DataManager.OnPortfolioChanged += DataProviderOnPortfolioChanged;
            _scriptingManager.ScriptingListUpdated += RefreshAllSignals;
            _scriptingManager.SignalInstanceUpdated += OnSignalInstanceUpdated;
            _scriptingManager.SignalBacktestUpdated += OnSignalBacktestUpdated;

            if (Portfolios.Count == 0)
            {
                //optional: wait for main form to finish initialization
                Task.Delay(1000).ContinueWith(x => CreatePortfolio(),
                    TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        #region Public Methods

        public void ShowSignalParamSpace(string signalName)
        {
            ExecuteWithDelay(() =>
            {
                var s = GetSignal(signalName);
                if (s != null)
                {
                    SelectedItem = s;
                    SelectedTabIndex = 3;
                }
            }, delay: 50);
        }

        public void ShowBacktestSettings(string signalName)
        {
            ExecuteWithDelay(() =>
            {
                var s = GetSignal(signalName);
                if (s != null && s.Parent != null)
                {
                    SelectedItem = s.Parent;
                    SelectedTabIndex = 4;
                }
            }, delay: 50);
        }

        public void ShowBacktestResults(string signalName)
        {
            ExecuteWithDelay(() =>
            {
                var s = GetSignal(signalName);
                if (s != null)
                {
                    SelectedItem = s;
                    SelectedTabIndex = 5;
                }
            }, delay: 50);
        }
        #endregion

        #region Events
        
        private void DataProviderOnPortfolioChanged(object sender, EventArgs<PortfolioActionEventArgs> e)
        {
            SyncPortfolioStrategyFolders(e.Value.PortfolioName, e.Value.IsRemoving);
            RefreshAllSignals();
            SelectItem(_selectedItemKey, delay: 50);
            _selectedItemKey = null;

            foreach (var s in _signalsToDeploy)  //auto-deploy new signals
            {
                var signal = GetSignal(s);
                if (signal != null && !signal.IsDeployed)
                {
                    DeploySignal(signal);
                    _selectedItemKey = signal.FullName;
                }
            }
            _signalsToDeploy.Clear();
        }

        private void RefreshAllSignals()
        {
            foreach (var p in Portfolios)
                foreach (var s in p.Strategies)
                {
                    LoadStrategyBacktestSettings(s);
                    foreach (var signal in s.Signals)
                    {
                        var sig = _scriptingManager.Signals.FirstOrDefault(i => i.FullName == signal.FullName);
                        if (sig != null)
                        {
                            signal.State = sig.State;
                            signal.Parameters = sig.Parameters.ToList();
                            signal.Selections = sig.Selections.ToList();

                            if (signal.Selections.Count == 0)
                                UpdateInstruments(signal);

                            if (signal.IsDeployed && !signal.IsParamSpaceSet)
                            {
                                signal.SetDefaultParamSpace(Path.Combine(Core.PathManager
                                    .GetDirectory4Signal(_userName, sig.FullName), "Parameters.csv"));
                            }
                        }
                    }
                }

            if (SelectedSignal != null)
                RefreshSignalGui();
        }
        
        private void OnSignalBacktestUpdated(object sender, EventArgs<List<BacktestResult>> e) =>
            UpdateSignalBacktestResults(e.Value);

        private void OnSignalInstanceUpdated(object sender, EventArgs<Signal> e) =>
            RefreshSignal(e.Value);

        #endregion //Events

        #region Private Methods

        private DataGridColumn CreateColumn(string header, int width, string property)
        {
            return new DataGridTextColumn
            {
                Header = header,
                MinWidth = width,
                Width = width,
                Binding = new Binding(property)
            };
        }

        private DataGridColumn CreateColumnWithFormat(string header, int width, string property, string format = "0.#####")
        {
            return new DataGridTextColumn
            {
                Header = header,
                MinWidth = width,
                Width = width,
                Binding = new Binding(property) { StringFormat = format }
            };
        }

        private void CreatePortfolio()
        {
            var name = "Portfolio ";
            int count = 1;
            while (Portfolios.Any(p => p.Name == name + count))
                count++;

            var dlgRes = Core.ViewFactory.ShowDialogView(new PortfolioDetailsViewModel(Core, new Portfolio
            {
                Accounts = new ObservableCollection<PortfolioAccount>(),
                Strategies = new ObservableCollection<Strategy>(),
                User = String.Empty,
                Name = name + count
            }));

            //insist on creating a portfolio
            if (Portfolios.Count == 0)
            {
                var msg = "You need to create a portfolio to continue using the application";
                var result = Core.ViewFactory.ShowMessage(msg, "Important",
                    MsgBoxButton.OKCancel, MsgBoxIcon.Information);
                if (result == DlgResult.OK)
                    CreatePortfolio();
                else
                    Core.ViewFactory.ExitApplication(0);
            }
            else
            {
                if (dlgRes == true)
                    SelectedItem = Portfolios.Last();
            }
        }

        private void ClonePortfolio()
        {
            if (!IsPortfolioSelected)
                return;

            var selected = (Portfolio)SelectedItem;
            var newName = selected.Name + " - Copy";
            var vm = new EditStringViewModel(newName, "New Portfolio Name", ResizeMode.NoResize, (string newValue) =>
            {
                if(Portfolios.Any(i => i.Name.Equals(newValue.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    _mainView.ShowNotification("Portfolio with same name already exist", 5);
                    return false;
                }
                return true;
            });
            var res = Core.ViewFactory.ShowDialogView(vm);
            if (res.Value == true && !String.IsNullOrWhiteSpace(vm.Value)
                && !Portfolios.Any(i => i.Name.Equals(vm.Value.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                newName = vm.Value.Trim();
                var clone = new Portfolio
                {
                    Name = newName,
                    User = selected.User,
                    BaseCurrency = selected.BaseCurrency,
                    Accounts = new ObservableCollection<PortfolioAccount>(selected.Accounts.Select(i => (PortfolioAccount)i.Clone())),
                    Strategies = new ObservableCollection<Strategy>()
                };

                foreach (var stgy in selected.Strategies)
                {
                    var s = stgy.Clone();
                    s.Parent = clone;
                    s.Signals = new ObservableCollection<Signal>();
                    foreach (var item in stgy.Signals)
                    {
                        var clonedSignal = CloneSignal(item, s);
                        s.Signals.Add(clonedSignal);
                        if (!_signalsToDeploy.Contains(clonedSignal.FullName))  //optional: auto-deploy signals on portfolio update
                            _signalsToDeploy.Add(clonedSignal.FullName);
                    }
                    clone.Strategies.Add(s);
                }

                Core.DataManager.CreatePortfolio(clone);
                _scriptingManager.UploadFolderToServer(UserFolder, newName, skipDlls: true);
            }
        }

        private void DeleteSelectedItem()
        {
            if (SelectedItem == null)
                return;

            if (SelectedPortfolio != null)
                DeletePortfolio(SelectedPortfolio);
            else if (SelectedStrategy != null)
                DeleteStrategy(SelectedStrategy);
            else if (SelectedSignal != null)
                DeleteSignal(SelectedSignal);
        }

        private void EditPortfolio()
        {
            if (SelectedPortfolio != null)
            {
                Core.ViewFactory.ShowDialogView(new PortfolioDetailsViewModel(Core,
                    SelectedPortfolio, true));
            }
        }

        private void DeletePortfolio(Portfolio portfolio)
        {
            if (portfolio == null || !Portfolios.Contains(portfolio))
                return;

            if (Portfolios.Count == 1)
            {
                Core.ViewFactory.ShowMessage("You can't use application without portfolios");
                return;
            }

            if (portfolio.Strategies.SelectMany(i => i.Signals).Any(i => i.IsDeployed))
            {
                Core.ViewFactory.ShowMessage("This portfolio has signals deployed on server.\r\n"
                    + "Please delete those signals first.", "Unable to delete portfolio",
                    MsgBoxButton.OK, MsgBoxIcon.Information);
                return;
            }

            if (Core.ViewFactory.ShowMessage($"Delete '{portfolio.Name}' portfolio and its contents?",
                "Question", MsgBoxButton.YesNo, MsgBoxIcon.Question) == DlgResult.Yes)
            {
                foreach (var s in portfolio.Strategies.SelectMany(i => i.Signals).Select(i => i.FullName))
                    DeleteSignalFiles(s);

                var directory = Path.Combine(UserFolder, portfolio.Name);
                try
                {
                    if (Directory.Exists(directory))
                        Directory.Delete(directory, true);
                }
                catch { }

                _selectedItemKey = Portfolios[0].Name != portfolio.Name ? Portfolios[0].Name : null;
                Core.DataManager.DeletePortfolio(portfolio);
            }
        }

        private void AddStrategy()
        {
            if (SelectedItem == null)
                return;

            Portfolio p = null;
            if (IsPortfolioSelected)
                p = SelectedPortfolio;
            else if (IsStrategySelected)
                p = SelectedStrategy.Parent;
            else if (IsSignalSelected)
                p = SelectedSignal.Parent.Parent;
            if (p == null)
                return;

            var name = "New Strategy";
            int count = 1;
            while (p.Strategies.Any(i => i.Name == name))
                name = "New Strategy " + count++;

            var newStrategy = new Strategy
            {
                Parent = p,
                Name = name,
                Datafeeds = new ObservableCollection<string>(),
                Signals = new ObservableCollection<Signal>()
            };

            Core.ViewFactory.ShowDialogView(new PortfolioDetailsViewModel(Core,
                newStrategy.Parent, true, newStrategy));
        }

        private void EditStrategy()
        {
            var strategy = SelectedStrategy;
            if (strategy?.Parent != null)
            {
                Core.ViewFactory.ShowDialogView(new PortfolioDetailsViewModel(Core,
                    strategy.Parent, true, strategy));
            }
        }

        private void DeleteStrategy(Strategy strategy)
        {
            if (strategy?.Parent == null)
                return;

            if (strategy.Signals.Any(i => i.IsDeployed))
            {
                Core.ViewFactory.ShowMessage("This strategy has signals deployed on server.\r\n"
                    + "Please delete those signals first.", "Unable to delete strategy",
                    MsgBoxButton.OK, MsgBoxIcon.Information);
                return;
            }

            if (Core.ViewFactory.ShowMessage($"Delete '{strategy.Name}' strategy?",
                "Question", MsgBoxButton.YesNo, MsgBoxIcon.Question) == DlgResult.Yes)
            {
                foreach (var item in strategy.Signals)
                    DeleteSignalFiles(item.FullName);

                var p = strategy.Parent;
                _selectedItemKey = p.Name;
                try
                {
                    Directory.Delete(Core.PathManager.GetDirectory4Strategy(_userName,
                        p.Name, strategy.Name), true);
                }
                catch { }

                _scriptingManager.DeleteFilesOnServer(p.Name + "\\" + strategy.Name);
                p.Strategies.Remove(strategy);
                Core.DataManager.UpdatePortfolio(p);
            }
        }

        private void CreateSignal()
        {
            Core.ViewFactory.ShowDialogView(new ScriptingSetupViewModel(Core,
                ScriptingType.Signal));
        }

        private Signal CloneSignal(Signal s, Strategy newParent)
        {
            //int count = 1;
            //var name = s.Name; // + count;
            //var names = Portfolios.SelectMany(i => i.Strategies)
            //    .SelectMany(i => i.Signals).Select(i => i.Name).ToList();
            //while (names.Contains(name, StringComparer.OrdinalIgnoreCase))
            //    name = s.Name + ++count;

            //create directory for the cloned signal settings, add/copy files
            var settingsDir = Path.Combine(UserFolder, s.FullName);
            var newSettingsDir = Path.Combine(Core.PathManager.PortfolioDirectory,
                _userName, newParent.Parent.Name, newParent.Name, s.Name);
            if (Directory.Exists(newSettingsDir))
                Directory.Delete(newSettingsDir, true);
            Directory.CreateDirectory(newSettingsDir);
            Extentions.CopyDirContents(settingsDir, newSettingsDir, 3);  //will copy signal + 2 parent directories

            return new Signal
            {
                FullName = $"{newParent.Parent.Name}\\{newParent.Name}\\{s.Name}",
                Parent = newParent,
                Parameters = new List<ScriptingParameterBase>(s.Parameters.Select(i => (ScriptingParameterBase)i.Clone())),
                Selections = new List<SignalSelection>(s.Selections.Select(i => (SignalSelection)i.Clone()))
            };
        }

        private void AddSignalToStrategy()
        {
            if (SelectedItem == null || SelectedItem is Portfolio)
                return;

            var strategy = SelectedStrategy ?? SelectedSignal?.Parent;
            if (strategy == null)
                return;

            var vm = new SelectSignalViewModel(Core.DataManager.ScriptingManager.GetScriptNamesFromDirectory(Core.PathManager.SignalsDirectory));
            var res = Core.ViewFactory.ShowDialogView(vm);

            if (res != null && res.Value && vm.SelectedSignals != null && vm.SelectedSignals.Any())
            {
                var duplicateSignal = new List<string>();
                foreach (var selectedSignal in vm.SelectedSignals)
                {
                    if (strategy.Signals.Any(s => s.Name.Equals(selectedSignal, StringComparison.OrdinalIgnoreCase)))
                    {
                        duplicateSignal.Add(selectedSignal);
                    }
                    else
                    {
                        var signal = new Signal
                        {
                            FullName = $"{strategy.Parent.Name}\\{strategy.Name}\\{selectedSignal}",
                            Parent = strategy
                        };

                        strategy.Signals.Add(signal);
                    }
                }
                if(duplicateSignal.Any())
                {
                    string message = (duplicateSignal.Count == 1 ? duplicateSignal.First() + " signal" : string.Join(",", duplicateSignal) + " signals") + " already added.";
                    _mainView.ShowNotification(message, 5);
                }

                Core.DataManager.UpdatePortfolio(strategy.Parent);
            }
        }

        private async void RunStrategySignals()
        {
            foreach (var signal in SelectedStrategy.Signals)
            {
                if (signal.State == SignalState.Stopped)
                {
                    if (signal.Selections.Count == 0)
                    {
                        continue;
                    }

                    var signalReqParameters = new SignalReqParams
                    {
                        FullName = signal.FullName,
                        StrategyParameters = new StrategyParams(signal.Parent),
                        IsSimulated = false,
                        Parameters = signal.Parameters,
                        Selections = signal.Selections,
                        Accounts = signal.Parent.Parent.Accounts.ToList()
                    };

                    await _scriptingManager.AddSignal(signalReqParameters,
                        Core.PathManager.GetDirectory4Signal(_userName, signal.FullName),
                        GetSignalSolutionDir(signal.Name, Core.PathManager.SignalsDirectory));
                }
                else if (signal.State == SignalState.Paused)
                {
                    _scriptingManager.InvokeSignalAction(signal.FullName,
                        SignalAction.SetSimulatedOff);
                }
            }
        }

        private void StopStrategySignals()
        {
            foreach (var signal in SelectedStrategy.Signals)
            {
                if (signal.State != SignalState.New && signal.State != SignalState.Stopped)
                {
                    _scriptingManager.InvokeSignalAction(signal.FullName, SignalAction.StopExecution);
                    signal.State = SignalState.Stopped;
                }
            }

            RefreshSignalGui();
        }

        private async void ClearStrategySignals()
        {
            var result = Core.ViewFactory.ShowMessage("Delete all strategy signals?",
                "Question", MsgBoxButton.YesNo, MsgBoxIcon.Question);
            if (result != DlgResult.Yes)
                return;

            var strategy = SelectedStrategy; //Selected strategy could be changed while task working

            await Task.Run(() =>
            {
                foreach (var signal in strategy.Signals)
                {
                    var existing = _scriptingManager.Signals.FirstOrDefault(p => p.FullName == signal.FullName);
                    if (existing != null && existing.IsDeployed)
                        _scriptingManager.RemoveSignal(signal.FullName);

                    _scriptingManager.RemoveSignalFromServer(signal.FullName);
                    DeleteSignalFiles(signal.FullName);
                }

            });

            strategy.Signals.Clear();
            _selectedItemKey = strategy.Parent.Name + "\\" + strategy.Name;
            Core.DataManager.UpdatePortfolio(strategy.Parent);
        }

        private void EditStrategyInstruments()
        {
            var vm = new EditStringViewModel("Slot,Datafeed,Symbol,TimeFactor,TimeFrame,BarCount,Level,Leverage,Slippage", "Strategy Instruments", ResizeMode.CanResize);
            var res = Core.ViewFactory.ShowDialogView(vm);

            if (res != null && res.Value && !string.IsNullOrEmpty(vm.Value))
            {
                foreach (var signal in SelectedStrategy.Signals)
                {
                    var dir = Core.PathManager.GetDirectory4Signal(_userName, signal.FullName);
                    var file = Path.Combine(dir, "Instruments.csv");
                    try
                    {
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                        if (!File.Exists(file))
                            File.WriteAllText(file, vm.Value);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }

        private void DeploySignal(Signal signal)
        {
            if (signal == null)
                return;

            var settingsDir = Core.PathManager.GetDirectory4Signal(_userName, signal.FullName);
            var errors = _scriptingManager.SendSignalToServer(GetSignalSolutionDir(signal.Name, Core.PathManager.SignalsDirectory), settingsDir);
            if (!string.IsNullOrEmpty(errors))
                Core.ViewFactory.ShowMessage(errors);
        }

        private void StartSignalExecution()
        {
            if (!IsSelectedSignalStartable)
                return;

            if (SelectedSignal.State == SignalState.Stopped)
            {
                UpdateInstruments();
                if (SelectedSignal.Selections.Count == 0)
                {
                    Core.ViewFactory.ShowMessage("Please add some instruments to this signal first");
                    return;
                }

                var s = SelectedSignal;
                bool? startSignal = Core.Settings.StartSignalWithoutConfiramtion;
                if (startSignal != true)
                {
                    using (var dlg = new CheckableDialogViewModel($"Start {s.Name} signal execution?", "Confirm"))
                    {
                        startSignal = Core.ViewFactory.ShowDialogView(dlg);
                        if (startSignal == true && dlg.IsChecked)
                            Core.Settings.StartSignalWithoutConfiramtion = startSignal;
                    }
                }

                if (startSignal == true)
                {
                    _scriptingManager.AddSignal(new SignalReqParams
                    {
                        FullName = s.FullName,
                        StrategyParameters = new StrategyParams(s.Parent),
                        IsSimulated = false,
                        Parameters = s.Parameters,
                        Selections = s.Selections,
                        Accounts = s.Parent.Parent.Accounts.ToList()
                    },
                    settingsPath: Core.PathManager.GetDirectory4Signal(_userName, s.FullName),
                    solutionPath: GetSignalSolutionDir(s.Name, Core.PathManager.SignalsDirectory));
                }
            }
            else if (SelectedSignal.State == SignalState.Paused)
            {
                _scriptingManager.InvokeSignalAction(SelectedSignal.FullName,
                    SignalAction.SetSimulatedOff);
            }
            else if (SelectedSignal.IsBacktesting)
            {
                Core.ViewFactory.ShowMessage("Please finish or abort the backtesting for this signal first");
            }
        }

        private void PauseSignalExecution()
        {
            if (SelectedSignal != null && SelectedSignal.State == SignalState.Working)
            {
                _scriptingManager.InvokeSignalAction(SelectedSignal.FullName,
                    SignalAction.SetSimulatedOn);
            }
        }

        private void StopSignalExecution()
        {
            if (IsSelectedSignalStoppable)
            {
                _scriptingManager.InvokeSignalAction(SelectedSignal.FullName,
                    SignalAction.StopExecution);
                SelectedSignal.State = SignalState.Stopped;
                RefreshSignalGui();
            }
        }

        private void StartSignalBacktest()
        {
            if (SelectedSignal == null)
                return;

            UpdateInstruments();

            if (SelectedSignal.State == SignalState.New)
                Core.ViewFactory.ShowMessage("Please deploy this signal to server first");
            else if (SelectedSignal.Selections.Count == 0)
                Core.ViewFactory.ShowMessage("Please add some instruments to this signal first");
            else if (SelectedSignal.State == SignalState.Working || SelectedSignal.State == SignalState.Paused)
                Core.ViewFactory.ShowMessage("Please stop this signal before launching a backtest");
            else if (SelectedSignal.State == SignalState.BacktestPaused)
                PauseOrResumeSignalBacktest();
            else if (SelectedSignal.State == SignalState.Stopped)
            {
                var s = SelectedSignal;
                if (s.NumericParamsCount == 0)
                {
                    Core.ViewFactory.ShowMessage("This signal doesn't have any parameters to be backtested",
                        "Invalid parameters space", MsgBoxButton.OK, MsgBoxIcon.Error);
                    return;
                }

                if (s.BacktestSettings == null && s.Parent.BacktestSettings == null)
                {
                    Core.ViewFactory.ShowMessage("Please define signal backtest settings first");
                    return;
                }

                if (s.IsDefaultParamSpaceUsed)
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

                var paramSpaceError = s.ValidateParamSpace();
                if (paramSpaceError != null)
                {
                    Core.ViewFactory.ShowMessage(paramSpaceError, "Invalid parameter space",
                        MsgBoxButton.OK, MsgBoxIcon.Error);
                }
                else
                {
                    s.BacktestResults.Clear();
                    _scriptingManager.AddSignal(new SignalReqParams
                    {
                        FullName = s.FullName,
                        StrategyParameters = new StrategyParams(s.Parent),
                        IsSimulated = false,
                        Parameters = s.Parameters,
                        Selections = s.Selections,
                        BacktestSettings = s.BacktestSettings,
                        StrategyBacktestSettings = s.Parent.BacktestSettings,
                        Accounts = s.Parent.Parent.Accounts.ToList()
                    },
                    settingsPath: Core.PathManager.GetDirectory4Signal(_userName, s.FullName),
                    solutionPath: GetSignalSolutionDir(s.Name, Core.PathManager.SignalsDirectory));
                }
            }
        }

        private void PauseOrResumeSignalBacktest()
        {
            if (SelectedSignal == null)
                return;

            if (SelectedSignal.State == SignalState.Backtesting)
            {
                _scriptingManager.InvokeSignalAction(SelectedSignal.FullName,
                    SignalAction.PauseBacktest);
            }
            else if (SelectedSignal.State == SignalState.BacktestPaused)
            {
                _scriptingManager.InvokeSignalAction(SelectedSignal.FullName,
                    SignalAction.ResumeBacktest);
            }
        }

        private void DeleteSignal(Signal signal)
        {
            if (signal?.Parent?.Parent == null)
                return;

            var result = Core.ViewFactory.ShowMessage($"Delete '{signal.Name}' signal?",
                "Question", MsgBoxButton.YesNo, MsgBoxIcon.Question);
            if (result != DlgResult.Yes)
                return;

            var existing = _scriptingManager.Signals.FirstOrDefault(p => p.FullName == signal.FullName);
            if (existing != null && existing.IsDeployed)
                _scriptingManager.RemoveSignal(signal.FullName);  //stops a working signal

            _scriptingManager.RemoveSignalFromServer(signal.FullName);
            DeleteSignalFiles(signal.FullName);

            var strategy = signal.Parent;
            strategy.Signals.Remove(signal);
            _selectedItemKey = strategy.Parent.Name + "\\" + strategy.Name;
            Core.DataManager.UpdatePortfolio(signal.Parent.Parent);
        }

        private void DeleteSignalFiles(string signalFullName, bool removeSolution = false)
        {
            string name = signalFullName;
            int idx = name.LastIndexOf('\\');
            if (removeSolution)
            {
                name = name.Substring(idx + 1);
                var solutionDir = Path.Combine(Core.PathManager.SignalsDirectory, name);
                if (Directory.Exists(solutionDir))
                {
                    try { Directory.Delete(solutionDir, true); }
                    catch { }
                }
            }

            var settingsDir = Core.PathManager.GetDirectory4Signal(_userName, signalFullName);
            if (Directory.Exists(settingsDir))
            {
                _scriptingManager.DeleteFilesOnServer(signalFullName);
                try { Directory.Delete(settingsDir, true); }
                catch { }
            }
        }

        private void UpdateSignalBacktestResults(List<BacktestResult> results)
        {
            if (results == null || results.Count == 0)
                return;

            Core.ViewFactory.BeginInvoke(() =>
            {
                var item = GetSignal(results[0].SignalFullName);
                if (item != null)
                {
                    foreach (var r in results)
                        item.BacktestResults.Add(r);

                    if (item.Selections.Count == 0)
                        UpdateInstruments(item);

                    if (item.FullName == SelectedSignal?.FullName)
                        RefreshSignalGui();
                }
            });
        }

        private void RefreshSignal(Signal signal)
        {
            foreach (var P in Portfolios)
                foreach (var S in P.Strategies)
                    foreach (var s in S.Signals)
                    {
                        if (s.FullName == signal.FullName)
                        {
                            s.State = signal.State;
                            s.Parameters = signal.Parameters.ToList();
                            if (signal.Selections != null && signal.Selections.Count > 0)
                                s.Selections = signal.Selections.ToList();
                            if (signal.BacktestResults.Any())
                            {
                                s.BacktestResults = signal.BacktestResults;
                                s.BacktestProgress = signal.BacktestProgress;
                            }

                            if (signal.FullName == SelectedSignal?.FullName)
                                RefreshSignalGui();
                            return;
                        }
                    }
        }

        private void RefreshSignalGui()
        {
            OnPropertyChanged("SelectedSignal");
            OnPropertyChanged("IsSignalSelected");
            OnPropertyChanged("IsSelectedSignalStartable");
            OnPropertyChanged("IsSelectedSignalPausable");
            OnPropertyChanged("IsSelectedSignalStoppable");
            OnPropertyChanged("BacktestResultColumns");
        }

       private void SyncPortfolioStrategyFolders(string portfolio, bool wasPortfolioRemoved = false)
        {
            if (wasPortfolioRemoved)  //remove portfolio folder
            {
                Core.PathManager.DeletePortfolioStrategySignalFolder(_userName, portfolio);
                return;
            }

            //sync strategies
            var p = Portfolios.FirstOrDefault(i => i.Name == portfolio);
            if (p?.Strategies != null && p.Strategies.Count > 0)
            {
                //remove deleted strategy folders
                var portfolioPath = Path.Combine(Core.PathManager.PortfolioDirectory,
                    _userName, portfolio);
                var strategies = p.Strategies.Select(i => i.Name).ToList();
                if (Directory.Exists(portfolioPath))
                {
                    foreach (var dir in Directory.GetDirectories(portfolioPath))
                    {
                        if (!strategies.Contains(Path.GetFileName(dir)))
                        {
                            try { Directory.Delete(dir); }
                            catch { }
                        }
                    }
                }

                //create signal directories
                foreach (var strategy in p.Strategies)
                    foreach (var sig in strategy.Signals)
                    {
                        var dir = Core.PathManager.GetDirectory4Signal(_userName, sig.FullName);
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                    }
            }
        }

        private void DeployStrategy()
        {
            if (SelectedStrategy == null || !SelectedStrategy.Signals.Any())
                return;

            if (SelectedStrategy.Signals.All(i => i.IsDeployed))
            {
                Core.ViewFactory.ShowMessage("This strategy is already deployed");
                _mainView.ShowNotification($"{SelectedStrategy.Name} strategy is already deployed", 5);
                return;
            }

            var res = Core.ViewFactory.ShowMessage($"Deploy {SelectedStrategy.Name} strategy signals to server?",
                "Confirmation", MsgBoxButton.YesNo, MsgBoxIcon.Question);
            if (res == DlgResult.Yes)
            {
                Task.Run(() =>
                {
                    foreach (var item in SelectedStrategy.Signals.Where(i => !i.IsDeployed))
                        DeploySignal(item);
                });
            }
        }

        private void EditInstruments()
        {
            if (SelectedSignal == null)
                return;

            var dir = Core.PathManager.GetDirectory4Signal(_userName, SelectedSignal.FullName);
            var file = Path.Combine(dir, "Instruments.csv");
            try
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                if (!File.Exists(file))
                    File.WriteAllText(file, "Slot,Datafeed,Symbol,TimeFactor,TimeFrame,BarCount,Level,Leverage,Slippage\r\n");
            }
            catch { }

            if (File.Exists(file))
            {
                var pcs = System.Diagnostics.Process.Start(file);

                //try to auto-update the instruments
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    while (pcs != null && !pcs.HasExited && SelectedSignal != null)
                        System.Threading.Thread.Sleep(200);

                    if (pcs != null && pcs.HasExited && SelectedSignal != null)
                        UpdateInstruments();
                });
            }
        }

        private void UpdateInstruments()
        {
            Core.ViewFactory.BeginInvoke(() =>
            {
                var error = UpdateInstruments(SelectedSignal);
                if (String.IsNullOrEmpty(error))
                    RefreshSignalGui();
            });
        }

        private string UpdateInstruments(Signal signal)
        {
            if (signal == null || signal.Parent == null || signal.Parent.Datafeeds.Count == 0)
                return null;

            var file = Path.Combine(Core.PathManager.GetDirectory4Signal(_userName,
                signal.FullName), "Instruments.csv");
            string error = null;
            var instruments = LoadInstruments(file, out error);
            signal.Selections.Clear();
            foreach (var item in instruments)
            {
                if (signal.Parent.Datafeeds.Any(i => i.Equals(item.DataFeed, StringComparison.OrdinalIgnoreCase)))
                    signal.Selections.Add(item);
            }
            return error;
        }

        private List<SignalSelection> LoadInstruments(string file, out string error)
        {
            error = null;
            if (!File.Exists(file))
                error = "Instruments file not found";
            else if (new FileInfo(file).Length < 2)
                error = "Instruments file is empty";

            if (error != null)
                return new List<SignalSelection>(0);

            string line = null;
            string[] items = null;
            var availableFeeds = Core.DataManager.DatafeedList;
            var result = new List<SignalSelection>();
            try
            {
                using (TextReader reader = new StreamReader(file))
                {

                    line = reader.ReadLine();
                    if (line == null || !line.ToLower().TrimStart().StartsWith("slot"))
                    {
                        error = "Signal instruments file doesn't contain valid data";
                        return new List<SignalSelection>(0);
                    }

                    while ((line = reader.ReadLine()) != null)
                    {
                        items = line.Split(new[] { ',', ';', '\t' }, StringSplitOptions.None);
                        if (items.Length > 5)
                        {
                            int slot = 0;
                            if (!Int32.TryParse(items[0], out slot))
                                continue;

                            var df = availableFeeds.FirstOrDefault(i => i.Equals(items[1].Trim(),
                                StringComparison.OrdinalIgnoreCase));
                            if (String.IsNullOrWhiteSpace(df))
                                continue;

                            var sym = items[2].Trim().ToUpper();
                            if (sym == String.Empty)
                                continue;

                            var tf = GetTimeframe(items[4], items[3]);
                            if (tf == null)
                                continue;

                            int count = 0;
                            Int32.TryParse(items[5], out count);

                            byte level = 0;
                            if (items.Length > 6)
                                Byte.TryParse(items[6], out level);

                            int leverage = 0;
                            if (items.Length > 7)
                                Int32.TryParse(items[7], out leverage);

                            decimal slippage = 0;
                            if (items.Length > 8)
                                Decimal.TryParse(items[8], out slippage);

                            if (!result.Any(i => i.MarketDataSlot == slot && i.Symbol == sym && i.DataFeed == df
                                && i.TimeFrame == tf.Item1 && i.Interval == tf.Item2))
                            {
                                result.Add(new SignalSelection(df, sym)
                                {
                                    MarketDataSlot = slot,
                                    TimeFrame = tf.Item1,
                                    Interval = tf.Item2,
                                    BarCount = count,
                                    Level = level,
                                    Leverage = leverage,
                                    Slippage = slippage
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = e.Message;
            }

            return result;
        }

        private void SaveInstruments()
        {
            if (SelectedSignal == null)  //|| !SelectedSignal.Selections.Any())
                return;

            var dir = Core.PathManager.GetDirectory4Signal(_userName,
                SelectedSignal.FullName);
            var file = Path.Combine(dir, "Instruments.csv");

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Slot,Datafeed,Symbol,TimeFactor,TimeFrame,BarCount,Level,Leverage,Slippage");
            foreach (var i in SelectedSignal.Selections.OrderBy(s => s.Symbol).ThenBy(s => s.TimeFrame))
            {
                sb.AppendLine($"0,{i.DataFeed},{i.Symbol},1,{i.TimeFrame},"
                    + $"{i.BarCount},{i.Level},{i.Leverage},{i.Slippage}");
            }

            try
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(file, sb.ToString());
            }
            catch (Exception e)
            {
                Core.ViewFactory.ShowMessage(e.Message, "Failed to save instruments data",
                    MsgBoxButton.OK, MsgBoxIcon.Error);
            }
        }

        private void AddAllSymbolsToSignal(string dataFeed)
        {
            if (SelectedSignal == null || String.IsNullOrWhiteSpace(dataFeed))
                return;

            var symbols = GetAllSelectionsForDatafeed(dataFeed);
            SelectedSignal.Selections.RemoveAll(i => i.DataFeed == dataFeed);
            SelectedSignal.Selections.AddRange(symbols ?? new List<SignalSelection>(0));

            RefreshSignalGui();

            SaveInstruments();
        }

        private List<SignalSelection> GetAllSelectionsForDatafeed(string datafeed)
        {
            var result = new List<SignalSelection>();
            foreach (var sym in Core.DataManager.GetAvailableSymbols(datafeed))
            {
                foreach (TimeFrame frame in Enum.GetValues(typeof(TimeFrame)))
                    result.Add(new SignalSelection(datafeed, sym) { TimeFrame = frame });
            }

            return result;
        }

        private void LoadStrategyBacktestSettings(Strategy strategy, bool loadSignalSettings = true)
        {
            if (strategy == null)
                return;

            var dir = Core.PathManager.GetDirectory4Strategy(_userName,
                strategy.Parent?.Name, strategy.Name);
            if (!Directory.Exists(dir))
                return;

            var file = Path.Combine(dir, "Backtest.xml");
            var settings = Extentions.Deserialize<StrategyBacktestSettings>(file);
            strategy.BacktestSettings = settings ?? new StrategyBacktestSettings();

            if (loadSignalSettings)
            {
                foreach (var s in strategy.Signals)
                    LoadSignalBacktestSettings(s);
            }
        }

        private void SaveStrategyBacktestSettings(Strategy strategy)
        {
            if (strategy == null || strategy.BacktestSettings == null)
                return;

            var dir = Core.PathManager.GetDirectory4Strategy(_userName,
                strategy.Parent?.Name, strategy.Name);
            if (!Directory.Exists(dir))
                return;

            var file = Path.Combine(dir, "Backtest.xml");
            try
            {
                Extentions.Serialize(strategy.BacktestSettings, file);
                _mainView.ShowNotification($"Settings for {strategy.Name} strategy have been saved", 5);
            }
            catch (Exception e)
            {
                Core.ViewFactory.ShowMessage(e.Message, "Failed to save strategy backtest settings",
                    MsgBoxButton.OK, MsgBoxIcon.Error);
            }
        }

        private void LoadSignalBacktestSettings(Signal signal)
        {
            if (signal == null)
                return;

            var dir = Core.PathManager.GetDirectory4Signal(_userName, signal.FullName);
            var file = Path.Combine(dir, "Backtest Settings.xml");
            if (!File.Exists(file))
                file = Path.Combine(dir, "Backtest.xml");

            if (File.Exists(file))
            {
                var settings = Extentions.Deserialize<SignalBacktestSettings>(file);
                signal.BacktestSettings = settings ?? new SignalBacktestSettings();
            }
        }

        private void SaveSignalBacktestSettings(Signal signal)
        {
            if (signal == null || signal.BacktestSettings == null)
                return;

            var file = Path.Combine(Core.PathManager.GetDirectory4Signal(_userName,
                signal.FullName), "Backtest Settings.xml");
            try
            {
                Extentions.Serialize(signal.BacktestSettings, file);
                _mainView.ShowNotification($"Settings for {signal.Name} signal have been saved", 5);
            }
            catch (Exception e)
            {
                Core.ViewFactory.ShowMessage(e.Message, "Failed to save signal backtest settings",
                    MsgBoxButton.OK, MsgBoxIcon.Error);
            }
        }

        private void LocateBarDataDirectory(Signal signal)
        {
            if (signal != null && signal.BacktestSettings != null)
            {
                var path = signal.BacktestSettings.BarDataDirectory;
                if (String.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                {
                    path = Path.Combine(Core.PathManager.RootDirectory, "Reports", signal.Name);
                    if (!Directory.Exists(path))
                    {
                        try { Directory.CreateDirectory(path); }
                        catch { }
                    }
                }
                if (!Directory.Exists(path))
                    path = Core.PathManager.RootDirectory;

                var result = Core.ViewFactory.ShowFolderDialog(path);
                if (result != null && Directory.Exists(result))
                    signal.BacktestSettings.BarDataDirectory = result;
            }
        }

        private void SaveBacktestResults()
        {
            if (SelectedSignal?.BacktestResults.Any() == true)
            {
                var file = Core.ViewFactory.ShowSaveFileDialog("XML Files|*.xml",
                    Core.PathManager.RootDirectory);
                if (!String.IsNullOrWhiteSpace(file))
                    SelectedSignal.SaveBacktestResults(file);
            }
        }


        private void ExportBacktestResults()
        {
            if (SelectedSignal?.BacktestResults.Any() == true)
            {
                var file = Core.ViewFactory.ShowSaveFileDialog("CSV Files|*.csv",
                    Core.PathManager.RootDirectory);
                if (!String.IsNullOrWhiteSpace(file))
                    SelectedSignal.ExportBacktestResultToCsv(file);
            }
        }

        private void LoadBacktestResults()
        {
            if (SelectedSignal != null)
            {
                var file = Core.ViewFactory.ShowOpenFileDialog("XML Files|*.xml",
                    Core.PathManager.RootDirectory);
                if (File.Exists(file))
                {
                    SelectedSignal.LoadBacktestResults(file);
                    RefreshSignalGui();
                }
            }
        }

        private void GetRecentBacktestResults()
        {
            if (SelectedSignal != null && !SelectedSignal.BacktestResults.Any() && !SelectedSignal.IsBacktesting)
                _scriptingManager.RequestBacktestResults(SelectedSignal.FullName);
        }

        private int GetSelectedTabIndexByItemType(object o)
        {
            if (o == null) return -1;

            //if (o is Portfolio) return 0;
            if (o is Strategy && _selectedTab != 0 && _selectedTab != 4) return 0;
            if (o is Signal && (_selectedTab == 0 || _selectedTab == 4)) return 1;

            return -1;
        }

        private void SelectClosestItemByTabIndex()
        {
            if (Portfolios == null || Portfolios.Count == 0)
                return;

            if (_selectedItem == null)
                _selectedItem = Portfolios[0];

            switch (_selectedTab)
            {
                case 0:  //select closest Portfolio parent
                    if (SelectedItem is Strategy)
                        SelectedItem = ((Strategy)SelectedItem).Parent;
                    else if (SelectedItem is Signal)
                        SelectedItem = ((Signal)SelectedItem).Parent.Parent;
                    break;

                case 1:
                case 5:  //select closest Strategy item
                    if (SelectedItem is Portfolio)
                    {
                        var p = SelectedItem as Portfolio;
                        if (p.Strategies.Any())
                            SelectedItem = p.Strategies[0];
                    }
                    else if (SelectedItem is Signal)
                    {
                        SelectedItem = ((Signal)SelectedItem).Parent;
                    }
                    break;

                case 2:
                case 3:
                case 4:
                case 6:  //select closest Signal item
                    if (SelectedItem is Portfolio)
                    {
                        var p = SelectedItem as Portfolio;
                        if (p.Strategies.Any() && p.Strategies[0].Signals.Any())
                            SelectedItem = p.Strategies[0].Signals[0];
                    }
                    else if (SelectedItem is Strategy)
                    {
                        var s = SelectedItem as Strategy;
                        if (s.Signals.Any())
                            SelectedItem = s.Signals[0];
                    }
                    break;
            }
        }

        private void SelectItem(string item, int delay = 0)
        {
            if (String.IsNullOrWhiteSpace(item))
                return;

            var parts = item.Split(Path.DirectorySeparatorChar);
            if (parts.Length > 3)
                return;

            object itemToSelect = null;
            switch (parts.Length)
            {
                case 1:
                    itemToSelect = Portfolios.FirstOrDefault(i => i.Name == item);
                    break;

                case 2:
                    itemToSelect = Portfolios.FirstOrDefault(i => i.Name == parts[0])
                        ?.Strategies.FirstOrDefault(i => i.Name == parts[1]);
                    break;

                case 3:
                    itemToSelect = GetSignal(item);
                    break;
            }

            if (itemToSelect != null)
                ExecuteWithDelay(() => SelectedItem = itemToSelect, delay);
        }

        private Signal GetSignal(string fullName)
        {
            return Portfolios.SelectMany(i => i.Strategies).SelectMany(i => i.Signals)
                .FirstOrDefault(i => i.FullName == fullName);
        }

        private void ExecuteWithDelay(Action action, int delay)
        {
            if (delay < 1)
                action();
            else
            {
                Task.Run(() =>
                {
                    System.Threading.Thread.Sleep(delay);
                    Core.ViewFactory.BeginInvoke(action);
                });
            }
        }

        private static string GetItemKey(object item)
        {
            if (item == null)
                return null;

            if (item is Signal)
                return ((Signal)item).FullName;

            if (item is Portfolio)
                return ((Portfolio)item).Name;

            if (item is Strategy)
            {
                var s = (Strategy)item;
                if (s.Parent != null)
                    return s.Parent.Name + "\\" + s.Name;
            }

            return null;
        }

        private static string GetSignalSolutionDir(string name, string signalsDir) => Path.Combine(signalsDir, name);

        //var slnFiles = Directory.GetFiles(signalsDir, name + ".sln", SearchOption.AllDirectories); //TODO why if we know path ^
        //return slnFiles.Any() ? Path.GetDirectoryName(slnFiles[0]) : null;

        private static Tuple<TimeFrame, byte> GetTimeframe(string period, string interval)
        {
            if (String.IsNullOrWhiteSpace(period) || String.IsNullOrWhiteSpace(interval))
                return null;

            byte size = 0;
            if (!Byte.TryParse(interval, out size))
                return null;

            if (size < 1)
                size = 1;

            var p = period.Trim().ToLower();
            if (p == "month" || p == "mn")
                return new Tuple<TimeFrame, byte>(TimeFrame.Month, 1);

            var c = p[0];
            switch (c)
            {
                case 't': return new Tuple<TimeFrame, byte>(TimeFrame.Tick, size);
                case 'm': return new Tuple<TimeFrame, byte>(TimeFrame.Minute, size);
                case 'h': return new Tuple<TimeFrame, byte>(TimeFrame.Hour, size);
                case 'd': return new Tuple<TimeFrame, byte>(TimeFrame.Day, size);
                case 'w': return new Tuple<TimeFrame, byte>(TimeFrame.Day, 7);
                default: return null;
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                Core.DataManager.OnPortfolioChanged -= DataProviderOnPortfolioChanged;
                _scriptingManager.ScriptingListUpdated -= RefreshAllSignals;
                _scriptingManager.SignalInstanceUpdated -= OnSignalInstanceUpdated;
                _scriptingManager.SignalBacktestUpdated -= OnSignalBacktestUpdated;
            }
        }

    }
}