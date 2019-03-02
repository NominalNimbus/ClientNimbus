using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using TradingClient.DataProvider.TradingService;
using TradingClient.Interfaces;
using AccountInfo = TradingClient.Data.Contracts.AccountInfo;
using AvailableBrokerInfo = TradingClient.Data.Contracts.AvailableBrokerInfo;
using Order = TradingClient.Data.Contracts.Order;
using Position = TradingClient.Data.Contracts.Position;
using Side = TradingClient.Data.Contracts.Side;
using TimeInForce = TradingClient.Data.Contracts.TimeInForce;
using OrderType = TradingClient.Data.Contracts.OrderType;
using CreateSimulatedBrokerAccountInfo = TradingClient.Data.Contracts.CreateSimulatedBrokerAccountInfo;

namespace TradingClient.DataProvider
{
    public class BrokerManager : IBrokerManager
    {
        private readonly List<Order> _orderActivity;
        private readonly List<Order> _pendingOrders;
        private readonly List<Order> _sessionOrderHistory;
        private readonly List<Position> _positions;
        private readonly ServiceConnector _connector;
        private readonly IDataManager _dataProvider;
        private readonly List<AvailableBrokerInfo> _brokers;
        private AutoResetEvent _initialize;
        private AutoResetEvent _login;
        private AutoResetEvent _logout;
        private string _loginResult;
        private string _logoutResult;
        private AccountInfo _defaultAccount;
        private readonly Dispatcher _dispatcher;

        #region Properties

        public bool IsActive => ActiveAccounts.Count > 0 && ActiveAccounts.Any(p => p.AvailableSymbols.Count > 0);

        public List<Order> OrderActivity
        {
            get
            {
                lock (_orderActivity)
                    return _orderActivity.ToList();
            }
        }

        public List<Order> PendingOrders
        {
            get
            {
                lock (_pendingOrders)
                    return _pendingOrders.ToList();
            }
        }

        public List<Order> SessionOrderHistory
        {
            get
            {
                lock (_sessionOrderHistory)
                    return _sessionOrderHistory.ToList();
            }
        }

        public ObservableCollection<AccountInfo> ActiveAccounts { get; private set; }

        public AccountInfo DefaultAccount
        {
            get => _defaultAccount;
            set
            {
                if (!Equals(value, _defaultAccount))
                {
                    _defaultAccount = value;
                    OnPropertyChanged(nameof(DefaultAccount));
                }
            }
        }

        public List<AvailableBrokerInfo> Brokers => _brokers.ToList();

        public List<Position> Positions => _positions.ToList();

        public IEnumerable<string> AvailableCurrencies => new List<string> { "EUR", "USD", "GBP" };

        #endregion

        #region Events

        public event EventHandler<EventArgs> OnAccountStateChanged;

        public event EventHandler<EventArgs<Order, string>> OnPlaceOrderError;

        private void RaiseOnPlaceOrderError(EventArgs<Order, string> e) =>
            OnPlaceOrderError?.Invoke(this, e);

        public event EventHandler<EventArgs<Order>> OrderUpdated;

        private void RaiseOrderUpdated(EventArgs<Order> e) => 
            OrderUpdated?.Invoke(this, e);

        public event EventHandler<EventArgs> ActiveOrdersChanged;

        private void RaiseActiveOrdersChanged() => 
            ActiveOrdersChanged?.Invoke(this, EventArgs.Empty);

        public event EventHandler<EventArgs> HistoricalOrdersChanged;

        private void RaiseHistoricalOrdersChanged() =>
            HistoricalOrdersChanged?.Invoke(this, EventArgs.Empty);

        public event EventHandler<EventArgs> PositionsChanged;

        private void RaisePositionsChanged() =>
            PositionsChanged?.Invoke(this, EventArgs.Empty);

        public event EventHandler<EventArgs<Position>> PositionUpdated;

        private void RaisePositionUpdated(EventArgs<Position> e) =>
            PositionUpdated?.Invoke(this, e);

        public event EventHandler<EventArgs<CreateSimulatedBrokerAccountInfo, string>> OnAddedNewBrokerAccount;

        private void RaiseNewBrokerAccount(EventArgs<CreateSimulatedBrokerAccountInfo, string> e) =>
            OnAddedNewBrokerAccount?.Invoke(this, e);


        #endregion

        public BrokerManager(ServiceConnector connector, IDataManager dataProvider)
        {
            _orderActivity = new List<Order>();
            _pendingOrders = new List<Order>();
            _sessionOrderHistory = new List<Order>();
            _positions = new List<Position>();
            ActiveAccounts = new ObservableCollection<AccountInfo>();
            ActiveAccounts.CollectionChanged += ActiveAccountsOnCollectionChanged;
            _brokers = new List<AvailableBrokerInfo>();
            _dispatcher = Dispatcher.CurrentDispatcher;

            _connector = connector;
            _dataProvider = dataProvider;
            _connector.BrokerList += ConnectorOnBrokerList;
            _connector.AccountChanged += ConnectorOnAccountChanged;

            _connector.OrderRejected += ConnectorOnOrderRejected;
            _connector.OrdersChanged += ConnectorOnOrdersChanged;
            _connector.HistoricalOrdersAdded += ConnectorOnHistoricalOrdersAdded;
            _connector.NewHistoricalOrder += ConnectorOnNewHistoricalOrder;
            _connector.OrdersUpdated += ConnectorOnOrdersUpdated;
            _connector.PositionsChanged += ConnectorOnPositionsChanged;
            _connector.PositionUpdated += ConnectorOnPositionUpdated;
            _connector.AvailableSecurities += ConnectorOnAvailableSecurities;
            _connector.AddedNewBrokerAccount += ConnectorOnAddedNewBrokerAccount;
        }

        private void ActiveAccountsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in args.NewItems)
                {
                    if (!(item is AccountInfo i))
                        continue;

                    i.PropertyChanged += AccountPropertyChanged;
                }
            }
            else if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in args.OldItems)
                {
                    if (!(item is AccountInfo i))
                        continue;

                    i.PropertyChanged -= AccountPropertyChanged;
                }
            }
        }

        private void AccountPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName.Equals("IsDefault"))
            {
                if (!(sender is AccountInfo account))
                    return;

                if (account.IsDefault)
                    DefaultAccount = account;
            }
        }

        public void Login(List<AccountInfo> accounts, Action<string> loginResult)
        {
            _connector.BrokerLogin += ConnectorOnBrokerLogin;
            _login = new AutoResetEvent(false);
            _connector.Send(new BrokersLoginRequest
            {
                Accounts = accounts.Select(p => new TradingClient.DataProvider.TradingService.AccountInfo
                {
                    ID = p.ID ?? string.Empty,
                    BrokerName = p.BrokerName,
                    DataFeedName = p.DataFeedName,
                    UserName = p.UserName,
                    Account = p.Account,
                    Password = p.Password,
                    Uri = p.Url
                }).ToList()
            });

            var res = _login.WaitOne(TimeSpan.FromSeconds(30));
            _login = null;

            _connector.BrokerLogin -= ConnectorOnBrokerLogin;

            if (res)
            {
                loginResult(_loginResult);
                _loginResult = string.Empty;
            }
            else
            {
                loginResult("Server does not respond");
            }
        }

        public string Logout(List<AccountInfo> accounts)
        {
            _connector.BrokerLogout += ConnectorOnBrokerLogout;
            _logout = new AutoResetEvent(false);
            _logoutResult = string.Empty;
            _connector.Send(new BrokerLogoutRequest
            {
                Accounts = accounts.Select(p => new TradingClient.DataProvider.TradingService.AccountInfo
                {
                    ID = p.ID,
                    BrokerName = p.BrokerName,
                    DataFeedName = p.DataFeedName,
                    UserName = p.UserName,
                    Account = p.Account,
                    Password = p.Password,
                    Uri = p.Url                    
                }).ToList()
            });

            var res = _logout.WaitOne(TimeSpan.FromSeconds(30));
            _logout = null;

            _connector.BrokerLogout -= ConnectorOnBrokerLogout;

            if (!res)
            {
                return "Server does not respond";
            }

            if (string.IsNullOrEmpty(_logoutResult))
            {
                InvokeInUI(() =>
                {
                    foreach (var account in accounts)
                        ActiveAccounts.Remove(account);
                });
            }

            return _logoutResult;
        }

        private void ConnectorOnBrokerLogout(object sender, EventArgs<string> eventArgs)
        {
            _logoutResult = eventArgs.Value;
            _logout?.Set();
        }

        public void Reconnect()
        {
            var tmp = ActiveAccounts.ToList();

            if (tmp.Count == 0)
                return;

            lock (_orderActivity)
                _orderActivity.Clear();

            lock (_pendingOrders)
                _pendingOrders.Clear();

            lock (_sessionOrderHistory)
                _sessionOrderHistory.Clear();

            lock (_positions)
                _positions.Clear();

            RaiseActiveOrdersChanged();
            RaiseHistoricalOrdersChanged();
            RaisePositionsChanged();


            Login(tmp, LoginResult);
        }

        private void LoginResult(string s)
        {
            if (string.IsNullOrEmpty(s))
                return;

            throw new Exception("Failed to reconnect broker accounts, try to restart TradingClient." + Environment.NewLine + s);
        }

        public void Initialize()
        {
            _initialize = new AutoResetEvent(false);
            _connector.Send(new TradingInfoRequest());
            var res = _initialize.WaitOne(TimeSpan.FromSeconds(30));
            _initialize = null;

            if (!res)
            {
                if (_brokers.Count == 0)
                    throw new Exception("No response from server");
            }
        }

        private void ConnectorOnBrokerLogin(object sender, EventArgs<string, List<AccountInfo>> eventArgs)
        {
            _loginResult = eventArgs.Value1;

            InvokeInUI(() =>
            {
                foreach (var account in eventArgs.Value2)
                {
                    if (!ActiveAccounts.Any(p => p.Equals(account)))
                        ActiveAccounts.Add(account);
                }
            });

            _connector.Send(new OrdersListRequest() { CountPerSymbol = 50, Skip = 0 });

            _login?.Set();
        }

        public void RequestMoreOrders(int count, int skip)
        {
            if (IsActive && count > 0 && skip >= 0)
                _connector.Send(new OrdersListRequest() { CountPerSymbol = count, Skip = skip });
        }

        public void CancelOrder(Order order, string account)
        {
            var request = new CancelOrderRequest
            {
                OrderID = order.ID,
                AccountId = account
            };

            _connector.Send(request);
        }

        public void CancelOrder(string id, string account)
        {
            var request = new CancelOrderRequest
            {
                OrderID = id,
                AccountId = account
            };

            _connector.Send(request);
        }

        public void PlaceOrder(Order order, string account)
        {
            var request = new PlaceOrderRequest
            {
                Order = new TradingClient.DataProvider.TradingService.Order
                {
                    UserID = order.ID,
                    Symbol = order.Symbol,
                    Quantity = order.Quantity,
                    OpenDate = DateTime.UtcNow,
                    OrderSide = ToDSSide(order.OrderSide),
                    TimeInForce = ToDSTIF(order.TimeInForce),
                    OrderType = ToDSOrderType(order.OrderType),
                    Price = order.Price,
                    SLOffset = order.SLOffset,
                    TPOffset = order.TPOffset,
                    ServerSide = order.ServerSide
                },
                AccountID = account
            };

            _connector.Send(request);
        }

        public void ClosePosition(string symbol, string account)
        {
            decimal qty;

            lock (_positions)
            {
                qty = _positions
                    .Where(p => p.AccountId.Equals(account) && p.Symbol.Equals(symbol, StringComparison.InvariantCultureIgnoreCase))
                    .Sum(p => Math.Abs(p.Quantity) * (p.Side == Side.Sell ? -1 : 1));
            }

            var request = new PlaceOrderRequest
            {
                Order = new TradingClient.DataProvider.TradingService.Order
                {
                    Symbol = symbol,
                    Quantity = Math.Abs(qty),
                    OpenDate = DateTime.UtcNow,
                    OrderSide = ToDSSide(qty > 0 ? Side.Sell : Side.Buy),
                    TimeInForce = TradingClient.DataProvider.TradingService.TimeInForce.FillOrKill,
                    OrderType = TradingClient.DataProvider.TradingService.OrderType.Market,
                    UserID = DateTime.UtcNow.Ticks.ToString(),
                    ClosingQty = Math.Abs(qty)
                },
                AccountID = account
            };

            _connector.Send(request);
        }

        public void ModifySL_TP(Order order, decimal? sl, decimal? tp, bool serverSide)
        {
            var request = new ModifyOrderRequest
            {
                OrderID = order.ID,
                AccountId = order.AccountId,
                SL = sl,
                TP = tp,
                IsServerSide = serverSide
            };

            _connector.Send(request);
        }

        public decimal GetMarginRateForSymbol(string symbol, string df)
        {
            var instrument = _dataProvider.GetInstrumentFromDataFeed(symbol, df);
            return instrument?.MarginRate ?? 0;
        }

        public void Dispose()
        {

        }

        #region Private Methods

        private void ConnectorOnBrokerList(object sender, EventArgs<List<AvailableBrokerInfo>> eventArgs)
        {
            _initialize?.Set();

            _brokers.AddRange(eventArgs.Value);

            _connector.BrokerList -= ConnectorOnBrokerList;
        }

        private void ConnectorOnAccountChanged(object sender, EventArgs<AccountInfo> args)
        {
            lock (ActiveAccounts)
            {
                var account = ActiveAccounts.FirstOrDefault(p => p.Equals(args.Value));
                if (account == null)
                    return;

                account.Balance = args.Value.Balance;
                account.Profit = args.Value.Profit;
                account.Margin = args.Value.Margin;
                account.Equity = args.Value.Equity;
                account.Coefficient = args.Value.Coefficient;
            }

            OnAccountStateChanged?.Invoke(this, new EventArgs());
        }

        private void ConnectorOnOrdersUpdated(object sender, EventArgs<string, List<TradingService.Order>> eventArgs)
        {
            var ordersToUpdate = new List<Order>(eventArgs.Value2.Count);
            lock (_orderActivity)
                lock (_pendingOrders)
                {
                    foreach (var order in eventArgs.Value2)
                    {
                        bool found = false;
                        foreach (var o in _orderActivity)
                        {
                            if (o.BrokerID == order.BrokerID)
                            {
                                o.CurrentPrice = order.CurrentPrice;
                                o.Price = order.AvgFillPrice;
                                o.ProfitPips = order.PipProfit;
                                o.Quantity = order.OpenQuantity;
                                o.FilledQuantity = order.OpenQuantity;
                                o.SLOffset = order.SLOffset;
                                o.TPOffset = order.TPOffset;
                                o.ServerSide = order.ServerSide;
                                o.Commission = order.Commission;
                                o.Profit = order.Profit;
                                ordersToUpdate.Add(o);
                                found = true;
                                break;
                            }
                        }

                        if (found)
                            continue;

                        foreach (var o in _pendingOrders)
                        {
                            if (o.BrokerID == order.BrokerID)
                            {
                                o.CurrentPrice = order.CurrentPrice;
                                o.Price = order.Price;
                                o.SLOffset = order.SLOffset;
                                o.Commission = order.Commission;
                                o.TPOffset = order.TPOffset;
                                o.FilledQuantity = order.FilledQuantity;
                                o.Quantity = order.Quantity;
                                o.ServerSide = order.ServerSide;
                                ordersToUpdate.Add(o);
                                break;
                            }
                        }
                    }
                }

            foreach (var o in ordersToUpdate)
                RaiseOrderUpdated(new EventArgs<Order>(o));
        }

        private void ConnectorOnAvailableSecurities(object sender, EventArgs<string, List<Data.Contracts.Security>> args)
        {
            lock (ActiveAccounts)
            {
                var account = ActiveAccounts.FirstOrDefault(p => p.ID.Equals(args.Value1));
                if (account == null)
                    return;

                account.AvailableSymbols.Clear();
                account.AvailableSymbols.AddRange(args.Value2);
            }

            OnAccountStateChanged?.Invoke(this, new EventArgs());
        }

        private void ConnectorOnAddedNewBrokerAccount(object sender, EventArgs<CreateSimulatedBrokerAccountInfo, string> e) =>
            RaiseNewBrokerAccount(e);

        private void ConnectorOnPositionUpdated(object sender, EventArgs<string, Position> args)
        {
            lock (_positions)
            {
                var pos = _positions.FirstOrDefault(p => p.Symbol.Equals(args.Value2.Symbol, StringComparison.InvariantCultureIgnoreCase) &&
                                                    p.AccountId == args.Value2.AccountId);

                if (pos != null)
                {
                    pos.Quantity = args.Value2.Quantity;
                    pos.Side = args.Value2.Side;
                    pos.AvgOpenCost = args.Value2.AvgOpenCost;
                    pos.ProfitPips = args.Value2.ProfitPips;
                    pos.Profit = args.Value2.Profit;
                    pos.CurrentPrice = args.Value2.CurrentPrice;
                    pos.Margin = args.Value2.Margin;

                    if (pos.Quantity != 0)
                    {
                        RaisePositionUpdated(new EventArgs<Position>(pos));
                    }
                    else
                    {
                        _positions.Remove(pos);
                        RaisePositionsChanged();
                    }
                }
            }
        }

        private void ConnectorOnPositionsChanged(object sender, EventArgs<string, List<Position>> eventArgs)
        {
            lock (_positions)
            {
                _positions.RemoveAll(p => p.AccountId.Equals(eventArgs.Value1));
                _positions.AddRange(eventArgs.Value2.ToList());
                RaisePositionsChanged();
            }
        }

        private void ConnectorOnHistoricalOrdersAdded(object sender, EventArgs<List<Order>> eventArgs)
        {
            lock (_sessionOrderHistory)
            {
                bool gotNewItems = false;
                foreach (var order in eventArgs.Value)
                {
                    if (_sessionOrderHistory.All(p => p.BrokerID != order.BrokerID))
                    {
                        _sessionOrderHistory.Add(order);
                        gotNewItems = true;
                    }
                }

                if (gotNewItems)
                    RaiseHistoricalOrdersChanged();
            }
        }

        private void ConnectorOnOrderRejected(object sender, EventArgs<Order, string> eventArgs)
        {
            RaiseOnPlaceOrderError(new EventArgs<Order, string>(eventArgs.Value1, eventArgs.Value2));
        }

        private void ConnectorOnNewHistoricalOrder(object sender, EventArgs<string, Order> eventArgs)
        {
            lock (_sessionOrderHistory)
            {
                _sessionOrderHistory.Add(eventArgs.Value2);
                RaiseHistoricalOrdersChanged();
            }
        }

        private void ConnectorOnOrdersChanged(object sender, EventArgs<string, List<Order>> e)
        {
            lock (_orderActivity)
            {
                _orderActivity.RemoveAll(p => p.AccountId.Equals(e.Value1));
                _orderActivity.AddRange(e.Value2.Where(p => p.OrderType == OrderType.Market));
            }

            lock (_pendingOrders)
            {
                _pendingOrders.RemoveAll(p => p.AccountId.Equals(e.Value1));
                _pendingOrders.AddRange(e.Value2.Where(p => p.OrderType != OrderType.Market));
            }

            RaiseActiveOrdersChanged();
        }

        private TradingClient.DataProvider.TradingService.Side ToDSSide(Side side)
        {
            switch (side)
            {
                case Side.Buy: return TradingClient.DataProvider.TradingService.Side.Buy;
                case Side.Sell: return TradingClient.DataProvider.TradingService.Side.Sell;
                default: throw new InvalidEnumArgumentException("Unknown order side.");
            }
        }

        private TradingClient.DataProvider.TradingService.TimeInForce ToDSTIF(TimeInForce tif)
        {
            switch (tif)
            {
                case TimeInForce.FillOrKill: return TradingClient.DataProvider.TradingService.TimeInForce.FillOrKill;
                case TimeInForce.ImmediateOrCancel: return TradingClient.DataProvider.TradingService.TimeInForce.ImmediateOrCancel;
                case TimeInForce.GoodForDay: return TradingClient.DataProvider.TradingService.TimeInForce.GoodForDay;
                case TimeInForce.GoodTilCancelled: return TradingClient.DataProvider.TradingService.TimeInForce.GoodTilCancelled;
                default: throw new InvalidEnumArgumentException("Unknown order TIF.");
            }
        }

        private TradingClient.DataProvider.TradingService.OrderType ToDSOrderType(OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.Limit: return TradingClient.DataProvider.TradingService.OrderType.Limit;
                case OrderType.Stop: return TradingClient.DataProvider.TradingService.OrderType.Stop;
                case OrderType.Market: return TradingClient.DataProvider.TradingService.OrderType.Market;
                default: throw new InvalidEnumArgumentException("Unknown order type.");
            }
        }

        private void InvokeInUI(Action action)
        {
            if (!_dispatcher.CheckAccess())
                _dispatcher.Invoke(action);
            else
                action();
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
