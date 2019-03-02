using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public class IndividualPositionsViewModel : DocumentViewModel, IIndividualPositionsViewModel
    {
        private readonly object _locker = new object();
        private bool _updateNeeded;
        private readonly System.Timers.Timer _timer;
        private bool _isTradingAllowed;
        private AccountInfo _selectedAccount;
        private IOrderItem _selectedOrder;

        #region Properties

        private IApplicationCore Core { get; }

        public override string Title => "Individual Positions";

        public override DocumentType DocumentType => DocumentType.IndividualPositions;

        public ObservableCollection<IOrderItem> Orders { get; private set; }

        public ObservableCollection<AccountInfo> Accounts { get; private set; }

        public AccountInfo SelectedAccount
        {
            get => _selectedAccount;
            set => SetPropertyValue(ref _selectedAccount, value, nameof(SelectedAccount), UpdateOrders);
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

        public bool IsTradingAllowed
        {
            get => _isTradingAllowed;
            set
            {
                if (value.Equals(_isTradingAllowed))
                    return;
                _isTradingAllowed = value;
                OnPropertyChanged("IsTradingAllowed");
            }
        }

        #endregion

        #region Commands

        public ICommand ModifyCommand { get; private set; }
        public ICommand DeleteTradeCommand { get; private set; }
        public ICommand ExportCommand { get; private set; }
        public ICommand PlaceOpposite { get; private set; }

        #endregion

        public IndividualPositionsViewModel(IApplicationCore core)
        {
            Core = core;

            Accounts = Core.DataManager.Broker.ActiveAccounts;
            
            lock (_locker)
                Orders = new ObservableCollection<IOrderItem>(Core.DataManager.Broker.OrderActivity
                    .Where(p => SelectedAccount != null && p.AccountId.Equals(SelectedAccount.ID)).Select(order =>
                {
                    var instrument = Core.DataManager.GetInstrumentFromBroker(order.Symbol, order.BrokerName);

                    return new OrderItem(order)
                    {
                        CurrentPrice = order.CurrentPrice,
                        CurrentPriceChange = 0,
                        ProfitPips = order.ProfitPips,
                        Profit = order.Profit,
                        Instrument = instrument, 
                        SL = order.SLOffset.HasValue ? order.SLOffset.Value : 0,
                        TP = order.TPOffset.HasValue ? order.TPOffset.Value : 0
                    };
                }
            ));

            if (Core.DataManager.Broker.DefaultAccount != null)
                SelectedAccount = Core.DataManager.Broker.DefaultAccount;
            else if (Accounts.Count > 0)
                SelectedAccount = Accounts.First();
            else
                return;

            IsTradingAllowed = Core.DataManager.Broker.IsActive;
            Core.DataManager.Broker.OnAccountStateChanged += BrokerOnOnAccountStateChanged;
            Core.DataManager.Broker.ActiveOrdersChanged += BrokerOnOrderStatusChanged;
            Core.DataManager.Broker.OrderUpdated += BrokerOnOrderUpdated;
            Core.DataManager.Broker.PropertyChanged += BrokerOnPropertyChanged;

            ModifyCommand = new RelayCommand(ModifyOrderExecute, () => SelectedOrder != null);
            PlaceOpposite = new RelayCommand(PlaceOppositeExecute, () => SelectedOrder != null);
            ExportCommand = new RelayCommand(ExportExecute);

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += UpdateOrders;
            _timer.Start();
        }

        private void BrokerOnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName.Equals("DefaultAccount"))
            {
                if (!(sender is IBrokerManager broker))
                    return;

                SelectedAccount = broker.DefaultAccount;
            }
        }

        private void BrokerOnOrderUpdated(object sender, EventArgs<Order> eventArgs)
        {
            Core.ViewFactory.Invoke(() =>
            {
                lock (_locker)
                {
                    var order = Orders.FirstOrDefault(p => p.Order.ID.Equals(eventArgs.Value.ID));
                    if (order == null)
                        return;

                    order.CurrentPriceChange = eventArgs.Value.CurrentPrice - order.CurrentPrice;
                    order.CurrentPrice = eventArgs.Value.CurrentPrice;
                    order.ProfitPips = eventArgs.Value.ProfitPips;
                    order.Profit = eventArgs.Value.Profit;
                    order.SL = eventArgs.Value.SLOffset ?? 0;
                    order.TP = eventArgs.Value.TPOffset ?? 0;
                    order.IsServerSide = eventArgs.Value.ServerSide;
                }
            });
        }

        private void PlaceOppositeExecute()
        {
            if (SelectedOrder != null)
            {
                var order = new Order("", SelectedOrder.Order.Symbol, SelectedOrder.Order.Quantity, 
                    OrderType.Market, SelectedOrder.Order.OrderSide != Side.Buy ? Side.Buy : Side.Sell, TimeInForce.GoodTilCancelled);
                using (var placeOrderVM = new PlaceOrderViewModel(Core, order))
                    Core.ViewFactory.ShowDialogView(placeOrderVM);
            }
        }

        private void BrokerOnOrderStatusChanged(object sender, EventArgs args)
        {
            lock (_locker)
                _updateNeeded = true;
        }

        private void UpdateOrders(object sender, EventArgs args)
        {
            if(_updateNeeded)
                UpdateOrders();
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
                    Profit = order.Profit,
                    Instrument = Core.DataManager.GetInstrumentFromBroker(order.Symbol, order.BrokerName),
                    SL = order.SLOffset ?? 0,
                    TP = order.TPOffset ?? 0,
                    IsServerSide = order.ServerSide
                }));

            Core.ViewFactory.Invoke(() =>
            {
                lock (_locker)
                {
                    var prevSelOrderID = SelectedOrder?.Order?.ID;
                    Orders.Clear();
                    foreach (var order in orders)
                        Orders.Add(order);
                    if (!String.IsNullOrEmpty(prevSelOrderID) && Orders.Count > 0)
                        SelectedOrder = Orders.FirstOrDefault(i => i.Order.ID == prevSelOrderID);
                }
            });
        }

        private void BrokerOnOnAccountStateChanged(object sender, EventArgs eventArgs)
        {
            IsTradingAllowed = Core.DataManager.Broker.IsActive;
        }

        private void ModifyOrderExecute()
        {
            var order2Modify = SelectedOrder.Order.Clone() as Order;
            using (var vm = new PlaceOrderViewModel(Core, order2Modify, isModified:true))
            {
                Core.ViewFactory.ShowDialogView(vm);
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                Core.DataManager.Broker.OnAccountStateChanged -= BrokerOnOnAccountStateChanged;
                Core.DataManager.Broker.ActiveOrdersChanged -= BrokerOnOrderStatusChanged;
                Core.DataManager.Broker.OrderUpdated -= BrokerOnOrderUpdated;
                if (_timer != null)
                {
                    _timer.Stop();
                    _timer.Elapsed -= UpdateOrders;
                }
                Core.DataManager.Broker.PropertyChanged -= BrokerOnPropertyChanged;
            }
        }

        private void ExportExecute()
        {
            var file = Core.ViewFactory.ShowSaveFileDialog("Report (*.xlsx)|*.xlsx",
                Core.PathManager.ExportedOrdersDirectory);

            if (string.IsNullOrEmpty(file))
                return;

            Task.Run(() =>
            {
                try
                {
                    ExcelExportManager.ExportOrders(file,"Individual Positions", Orders.Select(model => model.Order).ToList());
                }
                catch (Exception ex)
                {
                    Core.ViewFactory.ShowMessage("Failed to export to XLSX. Reason: " + ex.Message, 
                        "Error", MsgBoxButton.OK, MsgBoxIcon.Error);
                }
            });
        }
    }
}