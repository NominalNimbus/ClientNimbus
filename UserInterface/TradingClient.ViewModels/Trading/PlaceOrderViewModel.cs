using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public class PlaceOrderViewModel : ViewModelBase
    {
        #region Members

        
        private string _errors;
        private string _dataFeed;
        private TickData _lastTick;
        private Security _subscribedInstrument;
        private readonly List<Order> _lastOrders;
        private readonly Order _origOrder;
        private bool _serverSide;
        private bool _inProgress;

        #endregion //Members 

        #region Properties

        private IApplicationCore Core { get; }

        public string Title { get; }

        public bool IsModifying { get; }

        public bool IsPlaceNewOrder => !IsModifying;

        public PlaceOrderItem Order { get; private set; }

        public string Symbol
        {
            get => Order != null ? Order.Symbol : string.Empty;
            set
            {
                if (Order == null || value == Order.Symbol)
                    return;

                Order.Symbol = value;
                SelectedAccount = null;
                OnPropertyChanged(nameof(SelectedAccount));
                if (!IsModifying)
                {
                    foreach (var account in Accounts)
                    {
                        account.IsEnabled = account.AccountInfo.AvailableSymbols
                            .Any(q => q.Symbol.Equals(value, StringComparison.OrdinalIgnoreCase));
                        if (!account.IsEnabled && account.IsSelected)
                            account.IsSelected = false;
                    }
                }
                Resubscribe();
                OnPropertyChanged(nameof(Symbol));
            }
        }

        public string DataFeed
        {
            get => _dataFeed;
            set
            {
                if (value == _dataFeed)
                    return;

                _dataFeed = value;
                Instruments.Clear();
                var instruments =
                    Core.DataManager.Instruments.Where(
                                                         p =>
                                                         p.DataFeed.Equals(_dataFeed,
                                                             StringComparison.InvariantCultureIgnoreCase));
                foreach (var instrument in instruments)
                    Instruments.Add(instrument);

                Symbol = Instruments.FirstOrDefault()?.Symbol;

                Resubscribe();
                OnPropertyChanged(nameof(DataFeed));
            }
        }

        public decimal InstrumentQuantityIncrement => _subscribedInstrument?.QtyIncrement ?? 1.0M;

        public string InstrumentQuantityFormat => $"0.{new string('0', CalculateDigits(InstrumentQuantityIncrement))}";

        public decimal InstrumentPriceIncrement => _subscribedInstrument?.PriceIncrement ?? 1.0M;

        public string InstrumentPriceFormat => $"0.{new string('0', InstrumentDigits)}";

        public int InstrumentDigits => _subscribedInstrument?.Digits ?? 0;

        public ObservableCollection<Security> Instruments { get; private set; }

        public TickData LastTick
        {
            get => _lastTick;
            private set
            {
                SetPropertyValue(ref _lastTick, value, nameof(LastTick));
                OnPropertyChanged(nameof(LastListTick));
            }
        }

        public IEnumerable<TickData> LastListTick => new TickData[] { LastTick };


        public string Errors
        {
            get => _errors;
            set => SetPropertyValue(ref _errors, value, nameof(Errors));
        }

        public bool ServerSide
        {
            get => _serverSide;
            set => SetPropertyValue(ref _serverSide, value, nameof(ServerSide));
        }

        public ObservableCollection<Account> Accounts { get; private set; }

        public Account SelectedAccount { get; set; }

        public ObservableCollection<string> DataFeeds { get; private set; }

        #endregion //Properties

        #region Commands

        public ICommand OkCommand { get; private set; }

        public ICommand CancelCommand { get; private set; }

        public RelayCommand<Side> PlaceOrderCommand { get; private set; }

        public ICommand SetPriceCommand { get; private set; }

        #endregion Commands

        #region Constructor

        public PlaceOrderViewModel(IApplicationCore core, Order order, string datafeed = "", bool isModified = false)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            IsModifying = isModified;
            Title = (IsModifying ? "Edit " : "Place ") + "Order";

            Core = core ?? throw new ArgumentNullException(nameof(core));
            _lastOrders = new List<Order>();
            
            Accounts =
                    new ObservableCollection<Account>(
                        Core.DataManager.Broker.ActiveAccounts.Select(p => new Account
                        {
                            AccountInfo = p,
                            IsEnabled = p.AvailableSymbols.Any(q => q.Symbol.Equals(order.Symbol, StringComparison.InvariantCultureIgnoreCase)),
                            IsSelected = false
                        }));

            OkCommand = new RelayCommand(OkCommandExecute);
            PlaceOrderCommand = new RelayCommand<Side>(PlaceOrderCommandExecute);
            CancelCommand = new RelayCommand(CancelCommandExecute);
            SetPriceCommand = new RelayCommand(SetPriceCommandExecute);

            Instruments = new ObservableCollection<Security>();

            if (!string.IsNullOrEmpty(order.Symbol))
                LastTick = core.DataManager.GetTick(order.Symbol, order.BrokerName);

            if (LastTick == null)
                LastTick = new TickData();

            core.DataManager.OnNewTick += DataProviderOnNewQuote;

            if(isModified && string.IsNullOrEmpty(datafeed))
            {
                datafeed = Accounts.FirstOrDefault(a => a.AccountInfo.ID == order.AccountId)?.AccountInfo.DataFeedName;
            }

            DataFeeds = new ObservableCollection<string>(Core.DataManager.DatafeedList);
            if (DataFeeds.Count > 0)
                DataFeed = string.IsNullOrEmpty(datafeed) ? DataFeeds.First() : datafeed;

            Order = new PlaceOrderItem
            {
                OrderType = order.OrderType,
                Quantity = order.Quantity,
                SLOffset = order.SLOffset.HasValue ? order.SLOffset.Value : 0,
                TPOffset = order.TPOffset.HasValue ? order.TPOffset.Value : 0,
                Symbol = order.Symbol,
                TimeInForce = order.TimeInForce,
                OrderSide = order.OrderSide,
            };

            _origOrder = order;
            
            if (!String.IsNullOrEmpty(order.AccountId) && Accounts.Any(p => p.AccountInfo.ID.Equals(order.AccountId)))
            {
                foreach (var account in Accounts)
                    account.IsSelected = order.AccountId.Equals(account.AccountInfo.ID);                
            }

            Core.DataManager.Broker.OrderUpdated += BrokerOnOrderUpdated;
            Core.DataManager.Broker.ActiveOrdersChanged += BrokerOnActiveOrdersChanged;
            Core.DataManager.Broker.OnPlaceOrderError += BrokerOnOnPlaceOrderError;

            if (!string.IsNullOrWhiteSpace(Symbol) && !string.IsNullOrWhiteSpace(DataFeed))
                Subscribe(Symbol, DataFeed);

            ServerSide = order.ServerSide;
        }

        #endregion //Constructor

        #region Broker and DataFeed Events

        private void BrokerOnActiveOrdersChanged(object sender, EventArgs eventArgs)
        {
            lock (_lastOrders)
            {
                if (_lastOrders.Count > 0 && String.IsNullOrEmpty(Errors))
                    Exit();
            }
        }

        private void BrokerOnOnPlaceOrderError(object sender, EventArgs<Order, string> args)
        {
            var id = args.Value1.ID;
            if (id.EndsWith("_m"))
                id = id.Replace("_m", string.Empty);

            lock (_lastOrders)
            {
                if (_lastOrders.Count > 0 && _lastOrders.Any(p => p.ID.Equals(id)))
                {
                    Errors += args.Value1.AccountId + ": " + Environment.NewLine;

                    if (args.Value2.Equals("UNKNOWN_ORDER"))
                        Errors += "Can't modify SL or TP of current order because previous condition has triggered";
                    else if (args.Value2.Equals("OUTSIDE_ALLOWED_PRICE_RANGE"))
                        Errors += "Some of specified order price values are outside of allowed ranges";
                    else if (args.Value2.Equals("STOP_WOULD_BE_TRIGGERED_IMMEDIATELY"))
                        Errors += "Invalid order stops specified";
                    else
                        Errors += args.Value2;

                    _inProgress = false;
                }
            }
        }

        private void BrokerOnOrderUpdated(object sender, EventArgs<Order> args)
        {
            var id = args.Value.ID;
            if (id.EndsWith("_m"))
                id = id.Replace("_m", string.Empty);

            lock (_lastOrders)
            {
                if (_lastOrders.Count == 0)
                    return;

                var order = _lastOrders.FirstOrDefault(p => p.ID.Equals(id));

                if (order != null)
                    _lastOrders.Remove(order);

                if (_lastOrders.Count == 0)
                    Exit();
            }
        }
        
        private void DataProviderOnNewQuote(TickData quote)
        {
            if (quote.Symbol.EqualsValue(Symbol))
                LastTick = quote;
        }

        #endregion //Broker and DataFeed Events

        #region Command execution and privet methods

        private void OkCommandExecute()
        {
            if (!IsModifying)
                return;

            _lastOrders.Clear();
            Errors = string.Empty;
            _inProgress = true;

            
            {
                _lastOrders.Add(_origOrder);

                if (Order.SLOffset == 0)
                    Order.SLOffset = null;
                if (Order.TPOffset == 0)
                    Order.TPOffset = null;

                Core.DataManager.Broker.ModifySL_TP(_origOrder, Order.SLOffset, Order.TPOffset, ServerSide);
            }
           
        }

        private void PlaceOrderCommandExecute(Side side)
        {
            if (IsModifying)
                return;

            _lastOrders.Clear();
            Errors = string.Empty;
            _inProgress = true;

            if (Accounts.Any(p => p.IsSelected))
            {
                foreach (var item in Accounts.Where(p => p.IsSelected))
                {
                    Thread.Sleep(100);
                    var lastOrder = new Order(DateTime.UtcNow.Ticks.ToString(), Order.Symbol, Order.Quantity,
                        Order.OrderType, side, Order.Price, Order.TimeInForce)
                    {
                        SLOffset = Order.SLOffset > 0 ? Order.SLOffset : null,
                        TPOffset = Order.TPOffset > 0 ? Order.TPOffset : null,
                        BrokerName = item.AccountInfo.BrokerName,
                        AccountId = item.AccountInfo.ID,
                        ServerSide = ServerSide
                    };

                    _lastOrders.Add(lastOrder);
                    Core.DataManager.Broker.PlaceOrder(lastOrder.Clone() as Order, lastOrder.AccountId);
                }
            }
            else
            {
                Core.ViewFactory.ShowMessage("Please select account",
                       "Error", MsgBoxButton.OK, MsgBoxIcon.Warning);
            }
        }

        private void CancelCommandExecute() =>
            DialogResult = false;

        private void SetPriceCommandExecute()
        {
            if (LastTick == null)
                return;

            Order.Price = Order.OrderSide == Side.Buy ? LastTick.Ask : LastTick.Bid;
            OnPropertyChanged(nameof(Order));
        }

        private void Resubscribe()
        {
            if (!string.IsNullOrWhiteSpace(Symbol) && !string.IsNullOrWhiteSpace(DataFeed))
                Subscribe(Symbol, DataFeed);
        }

        private void Subscribe(string symbol, string df)
        {
            if (_subscribedInstrument != null)
                Core.DataManager.Unsubscribe(_subscribedInstrument.Symbol, _subscribedInstrument.DataFeed, this);

            var prevIncrement = InstrumentQuantityIncrement;
            _subscribedInstrument = Core.DataManager.GetInstrumentFromDataFeed(symbol, df);

            if(!IsModifying && prevIncrement != InstrumentQuantityIncrement)
            {
                Order.Quantity = InstrumentQuantityIncrement;
            }

            OnPropertyChanged(nameof(InstrumentQuantityIncrement));
            OnPropertyChanged(nameof(InstrumentQuantityFormat));
            OnPropertyChanged(nameof(InstrumentPriceIncrement));
            OnPropertyChanged(nameof(InstrumentPriceFormat));

            if (_subscribedInstrument != null)
            {
                LastTick = Core.DataManager.GetTick(symbol, df) ?? new TickData();
                Core.DataManager.Subscribe(symbol, df, this);
            }
        }

        private static int CalculateDigits(decimal value)
        {
            var digits = 0;
            do
            {
                value *= 10;
                ++digits;
            } while (Math.Abs(Math.Truncate(value) - value) > 0);

            digits = Math.Min(digits, 15);

            return digits;
        }

        private void Exit(bool result = true)
        {
            if (!DialogResult.HasValue)
            {
                _inProgress = false;
                DialogResult = result;
            }
        }
        
        #endregion //Command execution and privet methods

        #region Ovverrides

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (Core?.DataManager != null)
                {
                    if (_subscribedInstrument != null)
                        Core.DataManager.Unsubscribe(_subscribedInstrument.Symbol, _subscribedInstrument.DataFeed, this);
                    _subscribedInstrument = null;

                    Core.DataManager.OnNewTick -= DataProviderOnNewQuote;
                }

                if (Core?.DataManager?.Broker != null)
                {
                    Core.DataManager.Broker.OrderUpdated -= BrokerOnOrderUpdated;
                    Core.DataManager.Broker.ActiveOrdersChanged -= BrokerOnActiveOrdersChanged;
                    Core.DataManager.Broker.OnPlaceOrderError -= BrokerOnOnPlaceOrderError;
                }
            }
        }

        #endregion //Ovverrides
    }
}