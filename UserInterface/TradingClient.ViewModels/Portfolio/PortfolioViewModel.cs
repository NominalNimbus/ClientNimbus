using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Timers;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public class PortfolioViewModel : DocumentViewModel, IPortfolioViewModel
    {
        #region Members

        private readonly object _locker = new object();
        private bool _updateNeeded;
        
        private AccountInfo _selectedAccount;
        private IOrderItem _selectedOrder;
        private IOrderItem _selectedPendingOrder;
        private IPositionItem _selectedPosition;
        private int _lastSelectedID;
        private Portfolio _selectedPortfolio;
        private decimal _overallBalance;
        private decimal _overallMargin;
        private decimal _overallEquity;
        private decimal _overallProfit;
        private bool _overallIsMarginAccount;

        private readonly Timer _timer;
        private readonly Timer _updateHistoryTimer;

        private IApplicationCore Core { get; }

        #endregion //Members

        #region Bindings

        public override string Title => "Portfolio";

        public override DocumentType DocumentType => DocumentType.Portfolio;

        public ObservableCollection<IOrderItem> Orders { get; private set; }

        public ObservableCollection<IOrderItem> PendingOrders { get; private set; }

        public ObservableCollection<IOrderItem> HistoricalOrders { get; private set; }

        public ObservableCollection<IPositionItem> Positions { get; private set; }

        public ObservableCollection<AccountWithName> Accounts { get; private set; }

        public ObservableCollection<Portfolio> Portfolios { get; private set; }

        public AccountInfo SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                if(_selectedAccount == value)
                    return;

                _selectedAccount = value;

                UpdateOrders();
                UpdatePositions();
                UpdateHistory();
                OnPropertyChanged("SelectedAccount");
            }
        }

        public Portfolio SelectedPortfolio
        {
            get => _selectedPortfolio;
            set
            {
                if (_selectedPortfolio == value)
                    return;

                _selectedPortfolio = value;

                if (_selectedPortfolio != null)
                    _lastSelectedID = _selectedPortfolio.ID;

                SelectAccounts();
                OnPropertyChanged("SelectedPortfolio");
            }
        }

        public IOrderItem SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                if (_selectedOrder != value)
                {
                    _selectedOrder = value;
                    OnPropertyChanged("SelectedOrder");
                }
            }
        }

        public IOrderItem SelectedPendingOrder
        {
            get => _selectedPendingOrder;
            set
            {
                if (_selectedPendingOrder != value)
                {
                    _selectedPendingOrder = value;
                    OnPropertyChanged("SelectedPendingOrder");
                }
            }
        }

        public IPositionItem SelectedPosition
        {
            get => _selectedPosition;
            set
            {
                if (_selectedPosition != value)
                {
                    _selectedPosition = value;
                    OnPropertyChanged("SelectedPosition");
                }
            }
        }

        public decimal OverallBalance
        {
            get => _overallBalance;
            private set
            {
                if (_overallBalance == value)
                    return;

                _overallBalance = value;
                OnPropertyChanged("OverallBalance");
            }
        }

        public decimal OverallMargin
        {
            get => _overallMargin;
            private set
            {
                if (_overallMargin == value)
                    return;

                _overallMargin = value;
                OnPropertyChanged("OverallMargin");
            }
        }

        public decimal OverallEquity
        {
            get => _overallEquity;
            private set
            {
                if (_overallEquity == value)
                    return;

                _overallEquity = value;
                OnPropertyChanged("OverallEquity");
            }
        }

        public decimal OverallProfit
        {
            get => _overallProfit;
            private set
            {
                if (_overallProfit == value)
                    return;

                _overallProfit = value;
                OnPropertyChanged("OverallProfit");
            }
        }
        
        public bool OverallIsMarginAccounts
        {
            get => _overallIsMarginAccount;
            private set
            {
                if (_overallIsMarginAccount == value)
                    return;

                _overallIsMarginAccount = value;
                OnPropertyChanged("OverallIsMarginAccounts");
            }
        }

        public ICommand PlaceOpposite { get; private set; }

        public ICommand ClosePosition { get; private set; }

        public ICommand ModifyTradeCommand { get; private set; }

        public ICommand ModifyPendingCommand { get; private set; }

        public ICommand CancelPendingCommand { get; private set; }

        public ICommand CreatePortfolioCommand { get; private set; }

        public ICommand EditPortfolioCommand { get; private set; }

        public ICommand RemovePortfolioCommand { get; private set; }

        public ICommand ClearCommand { get; private set; }

        public bool IsTradingAllowed { get; private set; }

        #endregion

        #region Constructor

        public PortfolioViewModel(IApplicationCore core)
        {

            _lastSelectedID = -1;

            Core = core;
            Positions = new ObservableCollection<IPositionItem>();
            Orders = new ObservableCollection<IOrderItem>();
            PendingOrders = new ObservableCollection<IOrderItem>();
            HistoricalOrders = new ObservableCollection<IOrderItem>();
            Accounts = new ObservableCollection<AccountWithName>();
            Portfolios = Core.DataManager.Portfolios;

            if (Portfolios.Count > 0)
                SelectedPortfolio = Portfolios.First();
            
            _updateHistoryTimer = new Timer(200);
            _updateHistoryTimer.Elapsed += UpdateHistoryTimer_Elapsed;

            Core.DataManager.Broker.OnAccountStateChanged += BrokerOnOnAccountStateChanged;
            Core.DataManager.Broker.ActiveOrdersChanged += BrokerOnOrderStatusChanged;
            Core.DataManager.Broker.OrderUpdated += BrokerOnOrderUpdated;
            Core.DataManager.Broker.PositionsChanged += BrokerOnPositionChanged;
            Core.DataManager.Broker.PositionUpdated += BrokerOnPositionUpdated;
            Core.DataManager.Broker.HistoricalOrdersChanged += BrokerOnHistoricalOrdersChanged;
            Core.DataManager.OnPortfolioChanged += DataProviderOnPortfolioChanged;
            Core.DataManager.Broker.ActiveAccounts.CollectionChanged += ActiveAccountsOnCollectionChanged;

            ModifyTradeCommand = new RelayCommand(ModifyOrderExecute, () => SelectedOrder != null);
            ModifyPendingCommand = new RelayCommand(ModifyPendingExecute, () => SelectedPendingOrder != null);

            CancelPendingCommand = new RelayCommand(DeleteOrderExecute, () => SelectedPendingOrder != null);
            PlaceOpposite = new RelayCommand(PlaceOppositeExecute, () => SelectedOrder != null);
            ClosePosition = new RelayCommand(ClosePositionExecute, () => SelectedPosition != null);

            CreatePortfolioCommand = new RelayCommand(CreatePortfolioExecute);
            EditPortfolioCommand = new RelayCommand(EditPortfolioExecute, () => SelectedPortfolio != null);
            RemovePortfolioCommand = new RelayCommand(RemovePortfolioExecute, () => SelectedPortfolio != null);

            _timer = new Timer(1000);
            _timer.Elapsed += UpdateOrdersOnTimer;
            _timer.Start();

        }

     
        #endregion //Constructor

        #region Broker Event Handlers

        private void BrokerOnOnAccountStateChanged(object sender, EventArgs eventArgs)
        {
            IsTradingAllowed = Core.DataManager.Broker.IsActive;
        }

        private void BrokerOnOrderStatusChanged(object sender, EventArgs args)
        {
            lock (_locker)
                _updateNeeded = true;
        }

        private void BrokerOnOrderUpdated(object sender, EventArgs<Order> eventArgs)
        {
            var id = eventArgs.Value.ID;
            IOrderItem order = null, pending = null;
            lock (_locker)
            {
                foreach (var o in Orders)
                {
                    if (o.Order.ID == id)
                    {
                        order = o;
                        break;
                    }
                }

                foreach (var o in PendingOrders)
                {
                    if (o.Order.ID == id)
                    {
                        pending = o;
                        break;
                    }
                }
            }

            if (order == null && pending == null)
                return;

            Core.ViewFactory.Invoke(() =>
            {
                if (order != null)
                {
                    order.CurrentPriceChange = eventArgs.Value.CurrentPrice - order.CurrentPrice;
                    order.CurrentPrice = eventArgs.Value.CurrentPrice;
                    order.Profit = eventArgs.Value.Profit;
                    order.ProfitPips = eventArgs.Value.ProfitPips;
                    order.SL = eventArgs.Value.SLOffset ?? 0;
                    order.TP = eventArgs.Value.TPOffset ?? 0;
                    order.IsServerSide = eventArgs.Value.ServerSide;
                }

                if (pending != null)
                {
                    pending.CurrentPriceChange = eventArgs.Value.CurrentPrice - pending.CurrentPrice;
                    pending.CurrentPrice = eventArgs.Value.CurrentPrice;
                    pending.Profit = eventArgs.Value.Profit;
                    pending.FilledQty = eventArgs.Value.FilledQuantity;
                    pending.SL = eventArgs.Value.SLOffset ?? 0;
                    pending.TP = eventArgs.Value.TPOffset ?? 0;
                    pending.IsServerSide = eventArgs.Value.ServerSide;
                }
            });
        }

        private void ActiveAccountsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            SelectAccounts();
        }

        private void DataProviderOnPortfolioChanged(object sender, EventArgs<PortfolioActionEventArgs> eventArgs)
        {
            if (string.IsNullOrEmpty(eventArgs.Value.Error))
            {
                if (SelectedPortfolio == null && Portfolios.Any(p => p.ID == _lastSelectedID))
                    SelectedPortfolio = Portfolios.FirstOrDefault(p => p.ID == _lastSelectedID);
                else if (SelectedPortfolio == null && Portfolios.Count > 0)
                    SelectedPortfolio = Portfolios.First();

                if (eventArgs.Value.IsRemoving)
                {
                    Core.PathManager.DeletePortfolioStrategySignalFolder(Core.Settings.UserName,
                        eventArgs.Value.PortfolioName);
                }
            }
            else
            {
                Core.ViewFactory.ShowMessage(eventArgs.Value.Error, 
                    "Notification", MsgBoxButton.OK, MsgBoxIcon.Information);
            }
        }
        
        private void BrokerOnPositionChanged(object sender, EventArgs args)
        {
            UpdatePositions();
        }

        private void BrokerOnPositionUpdated(object sender, EventArgs<Position> eventArgs)
        {
            Core.ViewFactory.Invoke(() =>
            {
                lock (_locker)
                {
                    var pos = Positions.FirstOrDefault(p => p.Position.Symbol.Equals(eventArgs.Value.Symbol) && p.Position.AccountId == eventArgs.Value.AccountId);
                    if (pos == null)
                        return;

                    pos.CurrentPriceChange = eventArgs.Value.CurrentPrice - pos.CurrentPrice;
                    pos.CurrentPrice = eventArgs.Value.CurrentPrice;
                    pos.Profit = eventArgs.Value.Profit;
                    pos.ProfitPips = eventArgs.Value.ProfitPips;
                    pos.Side = eventArgs.Value.Side;
                    pos.Qty = Math.Abs(eventArgs.Value.Quantity);
                    pos.AvgPrice = eventArgs.Value.AvgOpenCost;
                    pos.Margin = eventArgs.Value.Margin;
                }
            });
        }

        private void BrokerOnHistoricalOrdersChanged(object sender, EventArgs eventArgs)
        {
            //UpdateHistory();
            _updateHistoryTimer.Start();
        }

        #endregion //Broker Event Handlers

        #region Commands Executions

        private void ModifyOrderExecute()
        {
            var order2Modify = SelectedOrder.Order.Clone() as Order;
            using (var vm = new PlaceOrderViewModel(Core, order2Modify, isModified:true))
            {
                Core.ViewFactory.ShowDialogView(vm);
            }
        }

        private void ModifyPendingExecute()
        {
            var order2Modify = SelectedPendingOrder.Order.Clone() as Order;
            using (var vm = new PlaceOrderViewModel(Core, order2Modify, isModified:true))
            {
                Core.ViewFactory.ShowDialogView(vm);
            }
        }

        private void DeleteOrderExecute()
        {
            Core.DataManager.Broker.CancelOrder(SelectedPendingOrder.Order, SelectedPendingOrder.Order.AccountId);
        }

        private void RemovePortfolioExecute()
        {
            var res = Core.ViewFactory
                .ShowMessage($"Do you really want to remove portfolio '{SelectedPortfolio.Name}'?",
                    "Question", MsgBoxButton.YesNoCancel, MsgBoxIcon.Question);

            if (res == DlgResult.Yes)
                Core.DataManager.DeletePortfolio(SelectedPortfolio);
        }

        private void EditPortfolioExecute()
        {
            Core.ViewFactory.ShowDialogView(new PortfolioDetailsViewModel(Core, SelectedPortfolio, true));
        }

        private void CreatePortfolioExecute()
        {
            var name = "Portfolio ";
            int count = 1;
            while (Portfolios.Any(p => p.Name.Equals(name + count)))
            {
                count++;
            }

            Core.ViewFactory.ShowDialogView(new PortfolioDetailsViewModel(Core, new Portfolio
            {
                Accounts = new ObservableCollection<PortfolioAccount>(),
                Strategies = new ObservableCollection<Strategy>(),
                User = string.Empty,
                Name = name + count
            }));
        }

        private void PlaceOppositeExecute()
        {
            if (SelectedOrder != null)
            {
                var order = new Order("", SelectedOrder.Order.Symbol, SelectedOrder.Order.Quantity,
                    OrderType.Market, SelectedOrder.Order.OrderSide != Side.Buy ? Side.Buy : Side.Sell, TimeInForce.GoodTilCancelled);
                order.BrokerName = SelectedOrder.Order.BrokerName;
                order.AccountId = SelectedOrder.Order.AccountId;

                using (var placeOrderVM = new PlaceOrderViewModel(Core, order))
                    Core.ViewFactory.ShowDialogView(placeOrderVM);
            }
        }

        private void ClosePositionExecute()
        {
            if (SelectedPosition != null && SelectedAccount != null)
                Core.DataManager.Broker.ClosePosition(SelectedPosition.Position.Symbol, SelectedAccount.ID);
        }

        #endregion //Commands Executions

        #region Timer members

        private void UpdateOrdersOnTimer(object sender, EventArgs args)
        {
            UpdateOverallInfo();

            if(!_updateNeeded)
                return;

            UpdateOrders();
        }

        private void UpdateHistoryTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _updateHistoryTimer.Stop();
            UpdateHistory();
        }


        #endregion //Timer members

        #region Helper members

        private void SelectAccounts()
        {
            Accounts.Clear();

            if (SelectedPortfolio == null)
            {
                return;
            }

            foreach (var availableAccount in SelectedPortfolio.Accounts.ToList())
            {
                var account =
                    Core.DataManager.Broker.ActiveAccounts.FirstOrDefault(q => q.BrokerName.Equals(availableAccount.BrokerName) && q.UserName.Equals(availableAccount.UserName));

                if (account == null)
                    continue;

                Accounts.Add(new AccountWithName
                {
                    Account = account,
                    Name = availableAccount.Name
                });
            }

            if (Accounts.Count > 0)
                SelectedAccount = Accounts.First().Account;
            else
                SelectedAccount = null;

            UpdateOverallInfo();
        }

        private decimal GetCoefficient(AccountInfo account)
        {
            if (SelectedPortfolio == null)
                return 1;

            switch (SelectedPortfolio.BaseCurrency)
            {
                case "EUR": return account.Coefficient.EUR;
                case "USD": return account.Coefficient.USD;
                case "GBP": return account.Coefficient.GBP;
                default: return 1;
            }
        }

        #endregion //Helper members

        #region Update members

        private void UpdatePositions()
        {
            var positions = new List<IPositionItem>(Core.DataManager.Broker.Positions
                .Where(p => SelectedAccount != null && p.AccountId == SelectedAccount.ID)
                .Select(position => new PositionItem(position)
                {
                    CurrentPrice = position.CurrentPrice,
                    CurrentPriceChange = 0,
                    Profit = position.Profit,
                    ProfitPips = position.ProfitPips,
                    Instrument = Core.DataManager.GetInstrumentFromBroker(position.Symbol, position.BrokerName),
                    AvgPrice = position.AvgOpenCost,
                    Qty = Math.Abs(position.Quantity),
                    Side = position.Side,
                }));

            Core.ViewFactory.Invoke(() =>
            {
                lock (_locker)
                {
                    var prevSelSymbol = SelectedPosition?.Instrument?.Symbol;
                    Positions.Clear();
                    foreach (var order in positions)
                        Positions.Add(order);
                    if (prevSelSymbol != null && Positions.Any())
                        SelectedPosition = Positions.FirstOrDefault(i => i.Instrument.Symbol == prevSelSymbol);
                }
            });
        }

        private void UpdateOrders()
        {
            _updateNeeded = false;

            var orders = new List<IOrderItem>(Core.DataManager.Broker.OrderActivity
                .Where(p => SelectedAccount != null && p.AccountId == SelectedAccount.ID)
                .Select(order => new OrderItem(order)
                {
                    CurrentPrice = order.CurrentPrice,
                    CurrentPriceChange = 0,
                    ProfitPips = order.ProfitPips,
                    Instrument = Core.DataManager.GetInstrumentFromBroker(order.Symbol, order.BrokerName),
                    FilledQty = order.FilledQuantity,
                    Profit = order.Profit,
                    SL = order.SLOffset ?? 0,
                    TP = order.TPOffset ?? 0,
                    IsServerSide = order.ServerSide
                }));

            var pending = new List<IOrderItem>(Core.DataManager.Broker.PendingOrders
                .Where(p => SelectedAccount != null && p.AccountId == SelectedAccount.ID)
                .Select(order => new OrderItem(order)
                {
                    CurrentPrice = order.CurrentPrice,
                    CurrentPriceChange = 0,
                    ProfitPips = order.ProfitPips,
                    Instrument = Core.DataManager.GetInstrumentFromBroker(order.Symbol, order.BrokerName),
                    Profit = order.Profit,
                    FilledQty = order.FilledQuantity,
                    SL = order.SLOffset ?? 0,
                    TP = order.TPOffset ?? 0,
                    IsServerSide = order.ServerSide
                }));

            Core.ViewFactory.Invoke(() =>
            {
                lock (_locker)
                {
                    var prevSelItemID = SelectedOrder?.Order?.ID;
                    Orders.Clear();
                    foreach (var order in orders)
                        Orders.Add(order);
                    if (!string.IsNullOrEmpty(prevSelItemID) && Orders.Count > 0)
                        SelectedOrder = Orders.FirstOrDefault(i => i.Order.ID == prevSelItemID);

                    prevSelItemID = SelectedPendingOrder?.Order?.ID;
                    PendingOrders.Clear();
                    foreach (var order in pending)
                        PendingOrders.Add(order);
                    if (!string.IsNullOrEmpty(prevSelItemID) && PendingOrders.Count > 0)
                        SelectedPendingOrder = PendingOrders.FirstOrDefault(i => i.Order.ID == prevSelItemID);
                }
            });
        }

        private void UpdateHistory()
        {
            var orders = new List<IOrderItem>(Core.DataManager.Broker.SessionOrderHistory.Where(p => SelectedAccount != null && p.AccountId.Equals(SelectedAccount.ID)).Select(order =>
            {
                var instrument = Core.DataManager.GetInstrumentFromBroker(order.Symbol, order.BrokerName);

                return new OrderItem(order)
                {
                    CurrentPrice = order.CurrentPrice,
                    CurrentPriceChange = 0,
                    ProfitPips = order.ProfitPips,
                    Instrument = instrument
                };
            }));

            orders.Sort((a, b) => b.Order.OpenDate.CompareTo(a.Order.OpenDate));

            Core.ViewFactory.Invoke(() =>
            {
                HistoricalOrders.Clear();
                foreach (var order in orders)
                    HistoricalOrders.Add(order);
            });
        }

        private void UpdateOverallInfo()
        {
            OverallBalance = Accounts.Sum(p => p.Account.Balance*GetCoefficient(p.Account));
            OverallEquity = Accounts.Sum(p => p.Account.Equity * GetCoefficient(p.Account));
            OverallMargin = Accounts.Sum(p => p.Account.Margin * GetCoefficient(p.Account));
            OverallProfit = Accounts.Sum(p => p.Account.Profit * GetCoefficient(p.Account));
            OverallIsMarginAccounts = Accounts.Any(p => p.Account.IsMarginAccount);
        }

        #endregion //Update members

        #region overrides members

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                Core.DataManager.Broker.OnAccountStateChanged -= BrokerOnOnAccountStateChanged;
                Core.DataManager.Broker.ActiveOrdersChanged -= BrokerOnOrderStatusChanged;
                Core.DataManager.Broker.OrderUpdated -= BrokerOnOrderUpdated;
                Core.DataManager.Broker.PositionsChanged -= BrokerOnPositionChanged;
                Core.DataManager.Broker.PositionUpdated -= BrokerOnPositionUpdated;
                Core.DataManager.Broker.HistoricalOrdersChanged -= BrokerOnHistoricalOrdersChanged;
                Core.DataManager.OnPortfolioChanged -= DataProviderOnPortfolioChanged;
                _timer.Stop();
                _timer.Elapsed -= UpdateOrdersOnTimer;
                _updateHistoryTimer.Stop();
                _updateHistoryTimer.Elapsed -= UpdateHistoryTimer_Elapsed;
                Core.DataManager.Broker.ActiveAccounts.CollectionChanged -= ActiveAccountsOnCollectionChanged;
            }
        }

        #endregion //overrides members
    }
}