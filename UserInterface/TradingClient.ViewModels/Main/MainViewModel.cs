using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ClosedXML.Excel;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using NLog;
using TradingClient.Common;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;
using TradingClient.ViewModelInterfaces;
using Xceed.Wpf.AvalonDock;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace TradingClient.ViewModels
{
    public class MainViewModel : ViewModelBase, IMainViewModel
    {
        #region Members

        private static readonly Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private IDocumentViewModel _activeElement;
        private DockingManager _dockingManager;
        private Dictionary<string, AlertViewModel> _alertsVMs;
        private Dictionary<string, SignalOutputViewModel> _signalOutputVMs;
        private string _notification;
        private bool _notificationVisible;

        private bool _isUserLoggedIn;
        private bool _isLogOpen;

        private bool _isCombinedPositionsOpen;
        private bool _isIndividualPositionsOpen;
        private bool _isScriptingLogOpen;
        private bool _isHistoryOrdersOpen;
        private bool _isTradingAllowed;
        private bool _isPendingOrdersOpen;
        private bool _isAccountInfoOpened;
        private bool _isDepthViewOpen;
        private bool _isPortfolioOpen;
        private bool _isAnalyzerOpen;
        private bool _isSignalsMgrOpen;
        private bool _isVisible;

        #endregion

        #region Commands members

        private RelayCommand _loadedCommand;
        private RelayCommand _closingCommand;

        private RelayCommand _watchListCommand;
        private RelayCommand _portfolioCommand;
        private RelayCommand _analyzerCommand;
        private RelayCommand _signalsManagerCommand;
        private RelayCommand _logCommand;
        private RelayCommand _depthViewCommand;
        private RelayCommand _scriptingLogCommand;

        private RelayCommand _hideNotificationCommand;

        private RelayCommand _saveWorkspaceCommand;
        private RelayCommand _loadWorkspaceCommand;

        private RelayCommand _placeTradeCommand;
        private RelayCommand _individualPositionsCommand;
        private RelayCommand _pendingOrdersCommand;
        private RelayCommand _historyOrdersCommand;
        private RelayCommand _combinedPositionsCommand;
        private RelayCommand _accountInfoCommand;
        private RelayCommand _exitCommand;
        private RelayCommand _showReportCommand;

        private RelayCommand _newIndicatorCommand;
        private RelayCommand _newSignalCommand;
        private RelayCommand _editIndicatorCommand;
        private RelayCommand _editSignalCommand;
        private RelayCommand _sendIndicatorToServerCommand;
        private RelayCommand _removeIndicatorFromServerCommand;

        private RelayCommand _editBrokerAccountsCommand;

        #endregion //Commands members

        #region Init

        public MainViewModel(IApplicationCore core)
        {
            Core = core;
            Core.DataManager.Broker.OnAccountStateChanged += BrokerOnOnAccountStateChanged;

            _alertsVMs = new Dictionary<string, AlertViewModel>();
            _signalOutputVMs = new Dictionary<string, SignalOutputViewModel>();
            Documents = new ObservableCollection<IDocumentViewModel>();
            Documents.CollectionChanged += Documents_CollectionChanged;

            Messenger.Default.Register<CloseDocumentMessage>(this, CloseDocumenntMessage);

            core.DataManager.OnConnectionStatusChanged += DataManagerOnConnectionStatusChanged;
            core.DataManager.OnDataFeedMessage += DatamanagerOnDataFeedMessage;
            core.DataManager.ScriptingManager.ScriptingMessage += OnScriptingMessage;
            core.ScriptingNotificationManager.NewNotification += OnNewNotification;
            core.ScriptingLogManager.OnNewLogMessage += OnNewLogMessage;
            core.ViewFactory.DepthViewAdded += ViewFactoryOnDepthViewAdded;

            IsTradingAllowed = Core.DataManager.Broker.IsActive;
        }

        #endregion //Init

        #region Document Commands

        public ICommand WatchListCommand => _watchListCommand ?? (_watchListCommand = new RelayCommand(() =>
        {
            AddDocument(new WatchListViewModel(Core));
        }));

        public ICommand PortfolioCommand => _portfolioCommand ?? (_portfolioCommand = new RelayCommand(() =>
        {
            AddDocument(new PortfolioViewModel(Core));
        }));

        public ICommand AnalyzerCommand => _analyzerCommand ?? (_analyzerCommand = new RelayCommand(() =>
        {
            InverseDocumentOpen(() => new AnalyzerViewModel(Core, this));
        }));

        public ICommand SignalsManagerCommand => _signalsManagerCommand ?? (_signalsManagerCommand = new RelayCommand(() =>
        {
            InverseDocumentOpen(() => new SignalsManagerViewModel(Core, this));
        }));

        public ICommand LogCommand => _logCommand ?? (_logCommand = new RelayCommand(() =>
        {
            InverseDocumentOpen(() => new LogViewModel(Core));
        }));

        public ICommand DepthViewCommand => _depthViewCommand ?? (_depthViewCommand = new RelayCommand(() =>
        {
            InverseDocumentOpen(() => new DepthViewModel(Core));
        }));

        public ICommand ScriptingLogCommand => _scriptingLogCommand ?? (_scriptingLogCommand = new RelayCommand(() =>
        {
            InverseDocumentOpen(() => new ScriptingLogViewModel(Core));
        }));
        
        public ICommand PendingOrdersCommand => _pendingOrdersCommand ?? (_pendingOrdersCommand = new RelayCommand(() =>
        {
            InverseDocumentOpen(() => new PendingOrdersViewModel(Core));
        }));

        public ICommand HistoryOrdersCommand => _historyOrdersCommand ?? (_historyOrdersCommand = new RelayCommand(() =>
        {
            InverseDocumentOpen(() => new HistoryOrdersViewModel(Core));
        }));

        public ICommand IndividualPositionsCommand => _individualPositionsCommand ?? (_individualPositionsCommand = new RelayCommand(() =>
        {
            InverseDocumentOpen(() => new IndividualPositionsViewModel(Core));
        }));

        public ICommand CombinedPositionsCommand => _combinedPositionsCommand ?? (_combinedPositionsCommand = new RelayCommand(() =>
        {
            InverseDocumentOpen(() => new CombinedPositionsViewModel(Core));
        }));

        public ICommand AccountInfoCommand => _accountInfoCommand ?? (_accountInfoCommand = new RelayCommand(() =>
        {
            InverseDocumentOpen(() => new AccountsViewModel(Core));
        }));

        #endregion //Document Commands

        #region Commands

        public ICommand HideNotificationCommand => _hideNotificationCommand ?? (_hideNotificationCommand = new RelayCommand(() =>
        {
            ShowNotification(null);
        }));

        public ICommand LoadedCommand => _loadedCommand ?? (_loadedCommand = new RelayCommand(() =>
        {
            _dockingManager = FindChild<DockingManager>(View);

            Task.Run(() =>
            {
                if (!Login())
                {
                    Core.ViewFactory.BeginInvoke(Application.Current.Shutdown);
                    return;
                }

                if (IsUserLoggedIn)
                {
                    Core.ViewFactory.BeginInvoke(() =>
                    {
                        LoadAndApplyWorkspace();

                        //force to create a portfolio via Analyzer view
                        if (!Core.DataManager.Portfolios.Any() && !IsAnalyzerOpen)
                            InverseDocumentOpen(() => new AnalyzerViewModel(Core, this));
                    });
                }
            });
        }));
        
        public ICommand ClosingCommand => _closingCommand ?? (_closingCommand = new RelayCommand(() =>
        {
            if (IsUserLoggedIn)
            {
                try
                {
                    Core.Settings.WorkspaceData = Core.ProtoSerializer.Serialize(GetWorkspace());
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to save settings");
                }
            }
            Dispose();
        }));

        public ICommand SaveWorkspaceCommand => _saveWorkspaceCommand ?? (_saveWorkspaceCommand = new RelayCommand(() =>
        {
            try
            {
                var file = Core.ViewFactory.ShowSaveFileDialog("Workspace (*.ws)|*.ws",
                    Core.PathManager.WorkspaceDirectory);

                if (string.IsNullOrEmpty(file))
                    return;

                var ws = GetWorkspace();

                File.WriteAllBytes(file, Core.ProtoSerializer.Serialize(ws));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save layout");
            }
        }));

        public ICommand LoadWorkspaceCommand => _loadWorkspaceCommand ?? (_loadWorkspaceCommand = new RelayCommand(() =>
        {
            try
            {
                var file = Core.ViewFactory.ShowOpenFileDialog("Workspace (*.ws)|*.ws",
                    Core.PathManager.WorkspaceDirectory);

                if (string.IsNullOrEmpty(file))
                    return;

                var w = Core.ProtoSerializer.Deserialize<Workspace>(File.ReadAllBytes(file));

                ApplyWorkspace(w);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load layout");
            }
        }));

        public ICommand PlaceTradeCommand => _placeTradeCommand ?? (_placeTradeCommand = new RelayCommand(() =>
        {
            var symbol = Core.DataManager.Instruments.First().Symbol;
            var order = new Order(DateTime.UtcNow.Ticks.ToString(), symbol, 1, OrderType.Market, Side.Buy,
                TimeInForce.GoodTilCancelled);
            using (var vm = new PlaceOrderViewModel(Core, order))
                Core.ViewFactory.ShowDialogView(vm);
        }));
        
        public ICommand ExitCommand => _exitCommand ?? (_exitCommand = new RelayCommand(() =>
        {
            Application.Current.Shutdown();
        }));
        
        public ICommand ShowReportCommand => _showReportCommand ?? (_showReportCommand = new RelayCommand(() =>
        {
            var strategies = new List<string>();

            foreach (var portfolio in Core.DataManager.Portfolios)
                foreach (var strategy in portfolio.Strategies)
                    foreach (var signal in strategy.Signals)
                        if (!strategies.Contains(signal.Name))
                            strategies.Add(signal.Name);

            using (var vm = new ShowScriptingReportViewModel(Core, strategies))
                Core.ViewFactory.ShowDialogView(vm);
        }));

        public ICommand NewIndicatorCommand => _newIndicatorCommand ?? (_newIndicatorCommand = new RelayCommand(() =>
        {
            Core.ViewFactory.ShowDialogView(new ScriptingSetupViewModel(Core, ScriptingType.Indicator));
        }));

        public ICommand NewSignalCommand => _newSignalCommand ?? (_newSignalCommand = new RelayCommand(() =>
        {
            Core.ViewFactory.ShowDialogView(new ScriptingSetupViewModel(Core, ScriptingType.Signal));
        }));

        public ICommand EditIndicatorCommand => _editIndicatorCommand ?? (_editIndicatorCommand = new RelayCommand(() =>
        {
            EditScripting(ScriptingType.Indicator);
        }));

        public ICommand EditSignalCommand => _editSignalCommand ?? (_editSignalCommand = new RelayCommand(() =>
        {
            EditScripting(ScriptingType.Signal);
        }));

        public ICommand SendIndicatorToServerCommand => _sendIndicatorToServerCommand ?? (_sendIndicatorToServerCommand = new RelayCommand(() =>
        {
            var path = Core.PathManager.IndicatorsDirectory;

            var openFileDialog = new OpenFileDialog
            {
                Filter = "Visual Studio Files|*.sln",
                InitialDirectory = path
            };

            var dialogResult = openFileDialog.ShowDialog();

            if (!dialogResult.HasValue || !dialogResult.Value) return;
            var fileName = openFileDialog.FileName;
            if (string.IsNullOrEmpty(fileName))
                return;

            if (Core.DataManager.ScriptingManager.Indicators.ContainsKey(Path.GetFileNameWithoutExtension(fileName)))
            {
                var res = Core.ViewFactory
                    .ShowMessage("User Indicator with same name already exist on server do you want to replace it?",
                        "Question", MsgBoxButton.YesNoCancel, MsgBoxIcon.Question);

                if (res != DlgResult.Yes)
                    return;
            }

            var errors = Core.DataManager.ScriptingManager.SendIndicatorToServer(fileName);
            if (!string.IsNullOrEmpty(errors))
                Core.ViewFactory.ShowMessage(errors);
        }));

        public ICommand RemoveIndicatorFromServerCommand => _removeIndicatorFromServerCommand ?? (_removeIndicatorFromServerCommand = new RelayCommand(() =>
        {
            var indicators = Core.DataManager.ScriptingManager.Indicators.Where(p =>
                    !Core.DataManager.ScriptingManager.DefaultIndicators.Contains(p.Key)).Select(p => p.Key);

            var vm = new DeleteItemViewModel(indicators, Core.DataManager.ScriptingManager.RemoveIndicatorFromServer, "Select custom indicator");
            Core.ViewFactory.ShowDialogView(vm);
        }));

        public ICommand EditBrokerAccountsCommand => _editBrokerAccountsCommand ?? (_editBrokerAccountsCommand = new RelayCommand(() =>
        {
            using (var vm = new BrokerLoginViewModel(Core, this, true))
            {
                if (Core.ViewFactory.ShowDialogView(vm) == true)
                    RefreshBrokerAccountSettings();
            }
        }));

        #endregion //Commands
        
        #region Document Properties

        public ObservableCollection<IDocumentViewModel> Documents { get; private set; }

        public IDocumentViewModel ActiveDocument
        {
            get => _activeElement;
            set => SetPropertyValue(ref _activeElement, value, nameof(ActiveDocument));
        }

        public bool IsPortfolioOpen
        {
            get => _isPortfolioOpen;
            set => SetPropertyValue(ref _isPortfolioOpen, value, nameof(IsPortfolioOpen));
        }

        public bool IsAnalyzerOpen
        {
            get => _isAnalyzerOpen;
            set => SetPropertyValue(ref _isAnalyzerOpen, value, nameof(IsAnalyzerOpen));
        }

        public bool IsSignalsMgrOpen
        {
            get => _isSignalsMgrOpen;
            set => SetPropertyValue(ref _isSignalsMgrOpen, value, nameof(IsSignalsMgrOpen));
        }

        public bool IsLogOpen
        {
            get => _isLogOpen;
            set => SetPropertyValue(ref _isLogOpen, value, nameof(IsLogOpen));
        }

        public bool IsDepthViewOpen
        {
            get => _isDepthViewOpen;
            set => SetPropertyValue(ref _isDepthViewOpen, value, nameof(IsDepthViewOpen));
        }

        public bool IsScriptingLogOpen
        {
            get => _isScriptingLogOpen;
            set => SetPropertyValue(ref _isScriptingLogOpen, value, nameof(IsScriptingLogOpen));
        }

        public bool IsPendingOrdersOpen
        {
            get => _isPendingOrdersOpen;
            set => SetPropertyValue(ref _isPendingOrdersOpen, value, nameof(IsPendingOrdersOpen));
        }

        public bool IsHistoryOrdersOpen
        {
            get => _isHistoryOrdersOpen;
            set => SetPropertyValue(ref _isHistoryOrdersOpen, value, nameof(IsHistoryOrdersOpen));
        }

        public bool IsIndividualPositionsOpen
        {
            get => _isIndividualPositionsOpen;
            set => SetPropertyValue(ref _isIndividualPositionsOpen, value, nameof(IsIndividualPositionsOpen));
        }

        public bool IsCombinedPositionsOpen
        {
            get => _isCombinedPositionsOpen;
            set => SetPropertyValue(ref _isCombinedPositionsOpen, value, nameof(IsCombinedPositionsOpen));
        }

        public bool IsAccountInfoOpened
        {
            get => _isAccountInfoOpened;
            set => SetPropertyValue(ref _isAccountInfoOpened, value, nameof(IsAccountInfoOpened));
        }

        #endregion //Document Properties

        #region Properties

        private IApplicationCore Core { get; }

        public Window View { get; set; }

        public bool IsUserLoggedIn
        {
            get => _isUserLoggedIn;
            set => SetPropertyValue(ref _isUserLoggedIn, value, nameof(IsUserLoggedIn));
        }
    
        public bool IsVisible
        {
            get => _isVisible;
            set => SetPropertyValue(ref _isVisible, value, nameof(IsVisible));
        }

        public bool IsTradingAllowed
        {
            get => _isTradingAllowed;
            set => SetPropertyValue(ref _isTradingAllowed, value, nameof(IsTradingAllowed));
        }

        public bool IsNotificationVisible
        {
            get => _notificationVisible;
            set => SetPropertyValue(ref _notificationVisible, value, nameof(IsNotificationVisible));
        }

        public string NotificationMessage
        {
            get => _notification;
            set => SetPropertyValue(ref _notification, value, nameof(NotificationMessage));
        }

        #endregion

        #region Worspaces methods

        private void LoadAndApplyWorkspace()
        {
            if (!IsUserLoggedIn)
                return;

            try
            {
                var workspace = Core.Settings.WorkspaceData == null || Core.Settings.WorkspaceData.Length < 5
                    ? GetDefaultWorkspace()
                    : Core.ProtoSerializer.Deserialize<Workspace>(Core.Settings.WorkspaceData)
                      ?? GetDefaultWorkspace();

                if (workspace != null)
                    ApplyWorkspace(workspace);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load settings");
            }
        }
        private Workspace GetDefaultWorkspace()
        {
            try
            {
                var file = Path.Combine(Directory.GetCurrentDirectory(), "Default.ws");
                if (File.Exists(file))
                    return Core.ProtoSerializer.Deserialize<Workspace>(File.ReadAllBytes(file));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load default workspace");
            }

            return null;
        }

        private void ApplyWorkspace(Workspace workspace)
        {
            try
            {
                CloseAllDocuments();
                LoadDockLayout(workspace.SerializedLayout, workspace);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load layout");
            }
        }

        private Workspace GetWorkspace()
        {
            var workspace = new Workspace
            {
                SerializedLayout = SaveDockLayout()
            };
            foreach (var tool in Documents)
            {
                var iw = new WorkspaceDocument { DocumentType = tool.DocumentType, SerializedData = tool.SaveWorkspaceData() };

                workspace.Documents.Add(tool.Id, iw);
            }

            return workspace;
        }

        private byte[] SaveDockLayout()
        {
            byte[] ret;
            using (var memoryStream = new MemoryStream())
            {
                new XmlLayoutSerializer(_dockingManager).Serialize(memoryStream);
                ret = memoryStream.ToArray();
            }

            return ret;
        }

        private void LoadDockLayout(byte[] bytes, object o)
        {
            using (var memoryStream = new MemoryStream())
            {
                memoryStream.Write(bytes, 0, bytes.Length);
                memoryStream.Seek(0, SeekOrigin.Begin);
                var layoutSerializer = new XmlLayoutSerializer(_dockingManager);

                layoutSerializer.LayoutSerializationCallback += (obj, arg) =>
                {
                    arg.Content = GetWorkspaceDocument(arg.Model.ContentId, o);
                };

                layoutSerializer.Deserialize(memoryStream);
            }
        }

        private object GetWorkspaceDocument(string contentId, object workspace)
        {
            if (!(workspace is Workspace w))
                throw new Exception();

            if (w.Documents.TryGetValue(contentId, out var viewWorkspace))
            {
                IDocumentViewModel tool = null;
                if (viewWorkspace.DocumentType == DocumentType.WatchList)
                    tool = new WatchListViewModel(Core);
                else if (viewWorkspace.DocumentType == DocumentType.Portfolio)
                    tool = new PortfolioViewModel(Core);
                else if (viewWorkspace.DocumentType == DocumentType.Analyzer)
                    tool = new AnalyzerViewModel(Core, this);
                else if (viewWorkspace.DocumentType == DocumentType.SignalsManager)
                    tool = new SignalsManagerViewModel(Core, this);
                else if (viewWorkspace.DocumentType == DocumentType.Log)
                    tool = new LogViewModel(Core);
                else if (viewWorkspace.DocumentType == DocumentType.IndividualPositions)
                    tool = new IndividualPositionsViewModel(Core);
                else if (viewWorkspace.DocumentType == DocumentType.DepthView)
                    tool = new DepthViewModel(Core);
                else if (viewWorkspace.DocumentType == DocumentType.HistoryOrders)
                    tool = new HistoryOrdersViewModel(Core);
                else if (viewWorkspace.DocumentType == DocumentType.AccountInfo)
                    tool = new AccountsViewModel(Core);
                else if (viewWorkspace.DocumentType == DocumentType.PendingOrders)
                    tool = new PendingOrdersViewModel(Core);
                else if (viewWorkspace.DocumentType == DocumentType.CombinedPositions)
                    tool = new CombinedPositionsViewModel(Core);
                else if (viewWorkspace.DocumentType == DocumentType.ScriptingLog)
                    tool = new ScriptingLogViewModel(Core);

                if (tool == null)
                {
                    Logger.Warn("Layout resolver failed to resolve an item with ID = " + contentId);
                    return null;
                }

                tool.LoadWorkspaceData(viewWorkspace.SerializedData);

                Documents.Add(tool);

                return tool;
            }

            Logger.Warn("Layout resolver failed to resolve an item");
            return null;
        }

        #endregion

        #region Events

        private void Documents_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IsAnalyzerOpen = Documents.Any(model => model is IAnalyzerViewModel);
            IsSignalsMgrOpen = Documents.Any(model => model is ISignalsManagerViewModel);
            IsLogOpen = Documents.Any(model => model is ILogViewModel);
            IsScriptingLogOpen = Documents.Any(model => model is IScriptingLogViewModel);
            IsPortfolioOpen = Documents.Any(model => model is IPortfolioViewModel);
            IsIndividualPositionsOpen = Documents.Any(model => model is IIndividualPositionsViewModel);
            IsCombinedPositionsOpen = Documents.Any(model => model is ICombinedPositionsViewModel);
            IsHistoryOrdersOpen = Documents.Any(model => model is IHistoryOrdersViewModel);
            IsPendingOrdersOpen = Documents.Any(model => model is IPendingOrdersViewModel);
            IsAccountInfoOpened = Documents.Any(model => model is IAccountsViewModel);
            IsDepthViewOpen = Documents.Any(model => model is IDepthViewModel);
        }

        private void ViewFactoryOnDepthViewAdded(object sender, EventArgs<string, string> eventArgs)
        {
            if (!(Documents.FirstOrDefault(model => model is IDepthViewModel) is IDepthViewModel depthModel))
            {
                var model = new DepthViewModel(Core);
                model.AddNewItem(eventArgs.Value1, eventArgs.Value2);
                InverseDocumentOpen(() => model);
            }
            else
            {
                ActiveDocument = depthModel;
                depthModel.AddNewItem(eventArgs.Value1, eventArgs.Value2);
            }
        }

        private void ActiveAccountsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var oldItem in args.OldItems)
                {
                    if (oldItem is AccountInfo)
                        CheckPortfolioAccount(oldItem as AccountInfo);
                }
            }
        }

        private void OnNewNotification(object sender, EventArgs<string, string> e)
        {
            var id = e.Value1;
            if (string.IsNullOrWhiteSpace(id))
                return;

            if (_alertsVMs == null)
                _alertsVMs = new Dictionary<string, AlertViewModel>();

            if (_alertsVMs.ContainsKey(id))
            {
                _alertsVMs[id].ShowNewItem(e.Value2);
            }
            else
            {
                _alertsVMs.Add(id, new AlertViewModel(Core, id));
                Core.ViewFactory.ShowView(_alertsVMs[id], () => { _alertsVMs.Remove(id); });
                _alertsVMs[id].Activated = true;
            }
        }

        private void OnNewLogMessage(object sender, ScriptingMessageEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.Writer) || _signalOutputVMs == null)
                return;
            
            if (_signalOutputVMs.TryGetValue(e.Writer, out var vm))
            {
                vm.ShowNewItem(e.Message);
            }
        }

        private void BrokerOnOnAccountStateChanged(object sender, EventArgs eventArgs) =>
            IsTradingAllowed = Core.DataManager.Broker.IsActive;

        private void DataManagerOnConnectionStatusChanged(ConnectionState status, string reason)
        {
            switch (status)
            {
                case ConnectionState.LostConnection:
                    ShowReconnectionView("Server connection lost, please reconnect or restart application", true);
                    break;
                case ConnectionState.ServerDisconnected:
                    ShowReconnectionView("Server connection lost, please restart application", false);
                    break;
                case ConnectionState.DisconnectedByAnotherUser:
                    ShowReconnectionView("Disconnected by server. Somebody connected with the same login", false);
                    break;
                case ConnectionState.Connect:
                    CommandManager.InvalidateRequerySuggested();
                    break;
            }
        }

        private void ShowReconnectionView(string msg, bool allowReconnect)
        {
            using (var vm = new ReconnectViewModel(Core, msg, allowReconnect))
            {
                var res = Core.ViewFactory.ShowDialogView(vm);
                if (res != true)
                    Core.ViewFactory.ExitApplication(0);
            }
        }

        private void DatamanagerOnDataFeedMessage(object sender, EventArgs<string> message) => 
            AppLogger.Info(message.Value);
        
        private void OnScriptingMessage(object sender, EventArgs<string> eventArgs) =>
            Core.ViewFactory.ShowMessage(eventArgs.Value);

        #endregion

        #region Public Methods

        public bool Login()
        {
            using (var vm = new LoginViewModel(Core))
            {
                if (Core.ViewFactory.ShowDialogView(vm) != true)
                    return false;
            }

            IsUserLoggedIn = Core.DataManager.IsConnected;

            if (IsUserLoggedIn && !Core.DataManager.IsOffline && Core.DataManager.Broker.Brokers.Count > 0)
            {
                if (!Core.Settings.AutoLoginBrokerAccounts || !AutoLoginBrokerAccounts(Core.Settings.Accounts))
                {
                    using (var vm = new BrokerLoginViewModel(Core, this))
                    {
                        if (Core.ViewFactory.ShowDialogView(vm) != true)
                            return false;
                    }
                }

                CheckPortfolioAccounts();
                Core.DataManager.Broker.ActiveAccounts.CollectionChanged += ActiveAccountsOnCollectionChanged;
            }

            IsVisible = true;
            return true;
        }

        public void ShowNotification(string message, byte displayDuration = 0)
        {
            if (IsNotificationVisible && NotificationMessage == message)
                return;

            Core.ViewFactory.BeginInvoke(() =>
            {
                bool hide = string.IsNullOrWhiteSpace(message);
                IsNotificationVisible = !hide;
                if (!hide)
                    NotificationMessage = message.Trim();
            });

            if (displayDuration > 0 && !string.IsNullOrWhiteSpace(message))
            {
                var thread = new System.Threading.Thread(() =>
                {
                    var tag = message;
                    for (byte i = 0; i < displayDuration && NotificationMessage == tag; i++)
                        System.Threading.Thread.Sleep(1000);
                    if (NotificationMessage == tag)
                        ShowNotification(null);
                });
                thread.Name = "Notification Display Delay";
                thread.IsBackground = true;
                thread.Start();
            }
        }

        public void ShowAlertsPopup(string scriptingId)
        {
            if (_alertsVMs != null && _alertsVMs.ContainsKey(scriptingId))
                _alertsVMs[scriptingId].Activated = true;
            else
                OnNewNotification(this, new EventArgs<string, string>(scriptingId, null));
        }

        public void ShowOutputPopup(string scriptId)
        {
            if (string.IsNullOrWhiteSpace(scriptId))
                return;

            if (!_signalOutputVMs.TryGetValue(scriptId, out var outputView))
            {
                outputView = new SignalOutputViewModel(Core, scriptId);
                _signalOutputVMs.Add(scriptId, outputView);
                Core.ViewFactory.ShowView(outputView, () => { _signalOutputVMs.Remove(scriptId); });
            }
            outputView.Activated = true;
        }

        public void ShowSignalParamSpace(string signalName)
        {
            if (!IsAnalyzerOpen)
                InverseDocumentOpen(() => new AnalyzerViewModel(Core, this));

            var a = Documents.FirstOrDefault(i => i is IAnalyzerViewModel);
            if (a != null)
            {
                a.IsActive = true;
                a.IsSelected = true;
                ((IAnalyzerViewModel) a).ShowSignalParamSpace(signalName);
            }
        }

        public void ShowBacktestSettings(string signalName)
        {
            if (!IsAnalyzerOpen)
                InverseDocumentOpen(() => new AnalyzerViewModel(Core, this));

            var a = Documents.FirstOrDefault(i => i is IAnalyzerViewModel);
            if (a != null)
            {
                a.IsActive = true;
                a.IsSelected = true;
                ((IAnalyzerViewModel) a).ShowBacktestSettings(signalName);
            }
        }

        public void ShowBacktestResults(string signalName)
        {
            if (!IsAnalyzerOpen)
                InverseDocumentOpen(() => new AnalyzerViewModel(Core, this));

            var a = Documents.FirstOrDefault(i => i is IAnalyzerViewModel);
            if (a != null)
            {
                a.IsActive = true;
                a.IsSelected = true;
                ((IAnalyzerViewModel) a).ShowBacktestResults(signalName);
            }
        }

        #endregion

        #region Document methods

        private void AddDocument(IDocumentViewModel tool)
        {
            Documents.Add(tool);
            tool.IsActive = true;
            tool.IsSelected = true;
        }

        private void InverseDocumentOpen<T>(Func<T> createDelegate) where T : IDocumentViewModel
        {
            var d = Documents.SingleOrDefault(vm => vm is T);
            if (d == null)
            {
                var doc = createDelegate.Invoke();
                if (doc == null)
                    throw new InvalidOperationException();
                AddDocument(doc);
            }
            else
                RemoveDocument(d);
        }

        private void RemoveDocument(IDocumentViewModel tool)
        {
            tool?.Dispose();
            Documents.Remove(tool);
        }

        private void CloseAllDocuments()
        {
            while (Documents.Count > 0)
            {
                Documents[0].Dispose();
                Documents.RemoveAt(0);
            }
        }
             
        private void CloseDocumenntMessage(CloseDocumentMessage message)
        {
            message.Document.Dispose();
            Documents.Remove(message.Document);
        }
        
        #endregion //Document methods
        
        #region Private Methods

        private bool AutoLoginBrokerAccounts(List<IAccountSetting> accounts)
        {
            if (accounts == null || accounts.Count == 0 || accounts.Any(i => string.IsNullOrEmpty(i.Key)))
                return false;

            try
            {
                Core.DataManager.Broker.Login(accounts.Select(i => i.ToAccountInfo()).ToList(), error =>
                {
                    if (string.IsNullOrEmpty(error))
                        RefreshBrokerAccountSettings();
                    else
                        Core.ViewFactory.ShowMessage(error, "Failed to log into broker account",
                            MsgBoxButton.OK, MsgBoxIcon.Warning);
                });
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Broker login failure: ");
                Core.ViewFactory.ShowMessage(e.Message, "Failed to log into broker account",
                    MsgBoxButton.OK, MsgBoxIcon.Warning);
                return false;
            }
        }

        private void RefreshBrokerAccountSettings()
        {
            Core.Settings.Accounts = Core.DataManager.Broker.ActiveAccounts
                .Select(i => new AccountSetting(i)).ToList<IAccountSetting>();
            if (!Core.DataManager.Broker.ActiveAccounts.Any())
                return;

            var broker = Core.DataManager.Broker
                .ActiveAccounts.FirstOrDefault(p => p.BrokerName == Core.Settings.DefaultBrokerName
                                                    && p.UserName == Core.Settings.DefaultBrokerAccount);
            if (broker == null)
                broker = Core.DataManager.Broker.ActiveAccounts.First();

            if (broker != null)
            {
                broker.IsDefault = true;
                Core.DataManager.Broker.DefaultAccount = broker;
            }
        }

        private void CheckPortfolioAccounts()
        {
            var builder = new StringBuilder();
            foreach (var portfolio in Core.DataManager.Portfolios.ToList())
            {
                var hasNotConnectedAccount = false;
                foreach (var account in portfolio.Accounts.ToList())
                {
                    if (!Core.DataManager.Broker.ActiveAccounts
                        .Any(p => p.BrokerName.Equals(account.BrokerName) && p.UserName.Equals(account.UserName)))
                    {
                        if (!hasNotConnectedAccount)
                        {
                            hasNotConnectedAccount = true;
                            builder.AppendFormat("Portfolio '{0}' has disconnected broker accounts: {1}",
                                portfolio.Name, Environment.NewLine);
                        }

                        builder.AppendFormat("Broker '{0}', username: '{1}'{2}",
                            account.BrokerName, account.UserName, Environment.NewLine);
                    }
                }
            }

            var str = builder.ToString();
            if (!string.IsNullOrEmpty(str))
                Core.ViewFactory.ShowMessage(str, "Warning", MsgBoxButton.OK, MsgBoxIcon.Warning);
        }

        private void CheckPortfolioAccount(AccountInfo info)
        {
            var builder = new StringBuilder();

            foreach (var portfolio in Core.DataManager.Portfolios.ToList())
            {
                if (portfolio.Accounts.Any( p => p.BrokerName?.Equals(info.BrokerName) == true && p.DataFeedName?.Equals(info.DataFeedName) == true 
                                           && p.UserName?.Equals(info.UserName) == true && p.Account?.Equals(info.Account) == true))
                    builder.AppendLine(portfolio.Name);
            }

            var str = builder.ToString();
            if (!string.IsNullOrEmpty(str))
            {
                var msg =
                    $"Disconnected broker account '{info.BrokerName} - {info.UserName}' used in portfolios:{Environment.NewLine}{str}";
                Core.ViewFactory.ShowMessage(msg, "Warning", MsgBoxButton.OK, MsgBoxIcon.Warning);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (Core != null && Core.DataManager != null)
                    Core.DataManager.OnConnectionStatusChanged -= DataManagerOnConnectionStatusChanged;

                Documents.ForEach(d => d.Dispose());
            }
        }

        private void EditScripting(ScriptingType type)
        {
            var path = Core.PathManager.IndicatorsDirectory;

            if (type == ScriptingType.Signal)
                path = Core.PathManager.SignalsDirectory;

            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Visual Studio Files|*.sln";
            openFileDialog.InitialDirectory = path;
            var dialogResult = openFileDialog.ShowDialog();

            if (!dialogResult.HasValue || !dialogResult.Value) return;
            var fileName = openFileDialog.FileName;
            if (string.IsNullOrEmpty(fileName))
                return;

            Process.Start(fileName);
        }

        private T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            if (parent is T) return parent as T;

            DependencyObject foundChild = null;

            (parent as FrameworkElement)?.ApplyTemplate();

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                foundChild = FindChild<T>(child);
                if (foundChild != null) break;
            }

            return foundChild as T;
        }

        #endregion
    }

}