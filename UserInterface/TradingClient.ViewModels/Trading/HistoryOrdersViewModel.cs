using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public class HistoryOrdersViewModel : DocumentViewModel, IHistoryOrdersViewModel
    {
        #region Members

        private AccountInfo _selectedAccount;
        private bool _canRequestMoreOrders;
        private readonly Timer _updateOrderTimer;

        #endregion //Members

        #region Properties

        private IApplicationCore Core { get; }

        public override string Title => "History Orders";

        public override DocumentType DocumentType => DocumentType.HistoryOrders;

        public ObservableCollection<IOrderItem> Orders { get; private set; }

        public IOrderItem SelectedOrder { get; set; }

        public bool IsTradingAllowed { get; private set; }

        public ObservableCollection<AccountInfo> Accounts { get; private set; }

        public AccountInfo SelectedAccount
        {
            get => _selectedAccount;
            set => SetPropertyValue(ref _selectedAccount, value, nameof(SelectedAccount), UpdateOrders);
        }

        public bool CanRequestMoreOrders
        {
            get => _canRequestMoreOrders;
            private set => SetPropertyValue(ref _canRequestMoreOrders, value, nameof(CanRequestMoreOrders));
        }

        #endregion //Properties

        #region Commands

        public ICommand ModifyCommand { get; private set; }

        public ICommand ExportCommand { get; private set; }

        public ICommand ClearCommand { get; private set; }

        public ICommand ShowMoreOrdersCommand { get; private set; }

        #endregion

        #region Constructor

        public HistoryOrdersViewModel(IApplicationCore core)
        {
            Core = core;

            Accounts = Core.DataManager.Broker.ActiveAccounts;
            Orders = new ObservableCollection<IOrderItem>();

            if (Core.DataManager.Broker.DefaultAccount != null)
                SelectedAccount = Core.DataManager.Broker.DefaultAccount;
            else if (Accounts.Count > 0)
                SelectedAccount = Accounts.First();

            _updateOrderTimer = new Timer(200);
            _updateOrderTimer.Elapsed += UpdateOrderTimer_Elapsed;

            IsTradingAllowed = Core.DataManager.Broker.IsActive;
            Core.DataManager.Broker.HistoricalOrdersChanged += BrokerOnOrderUpdated;
            Core.DataManager.Broker.PropertyChanged += BrokerOnPropertyChanged;

            ExportCommand = new RelayCommand(ExportCommandExecute);
            ClearCommand = new RelayCommand(ClearCommandExecute,() => Orders.Any());
            ShowMoreOrdersCommand = new RelayCommand(ShowMoreOrdersExecute, () => Orders.Any());
        }

    
        #endregion //Constructor

        #region Broker Event Handlers

        private void BrokerOnOrderUpdated(object sender, EventArgs args) =>
            _updateOrderTimer.Start();

        private void BrokerOnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName.Equals("DefaultAccount"))
            {
                if (!(sender is IBrokerManager broker))
                    return;

                SelectedAccount = broker.DefaultAccount;
            }
        }

        #endregion //Broker Event Handlers

        #region Commands methods

        private void ExportCommandExecute()
        {
            var file = Core.ViewFactory.ShowSaveFileDialog("History Report (*.xlsx)|*.xlsx",
                Core.PathManager.ExportedOrdersDirectory);

            if (string.IsNullOrEmpty(file))
                return;

            Task.Run(() =>
            {
                try
                {
                    ExcelExportManager.ExportOrders(file, "History Orders", Orders.Select(model => model.Order).ToList());
                }
                catch (Exception ex)
                {
                    Core.ViewFactory.ShowMessage("Failed to export to XLSX. Reason: " + ex.Message,
                        "Error", MsgBoxButton.OK, MsgBoxIcon.Error);
                }
            });
        }

        private void ClearCommandExecute()
        {
            var result = MessageBox.Show("Clear orders history?", "Clear", MessageBoxButton.YesNoCancel,
                   MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            lock (Core)
            {
                Core.DataManager.Broker.SessionOrderHistory.RemoveAll(p => SelectedAccount != null && p.AccountId.Equals(SelectedAccount.ID));
                Orders.Clear();
            }
        }

        private void ShowMoreOrdersExecute()
        {
            CanRequestMoreOrders = false;
            lock (Core)
            {
                Core.DataManager.Broker.RequestMoreOrders(50,
                    Orders.GroupBy(o => o.Instrument).Max(g => g.Count()));
            }
        }

        #endregion //Commands methods

        #region Helper methods

        private void UpdateOrderTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _updateOrderTimer.Stop();
            UpdateOrders();
        }

        private void UpdateOrders()
        {
            lock (Core)
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
                    Orders.Clear();
                    foreach (var order in orders)
                        Orders.Add(order);
                    CanRequestMoreOrders = true;
                });
            }
        }

        #endregion //Helper methods

        #region overrides methods

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                Core.DataManager.Broker.HistoricalOrdersChanged -= BrokerOnOrderUpdated;
                Core.DataManager.Broker.PropertyChanged -= BrokerOnPropertyChanged;

                _updateOrderTimer.Stop();
                _updateOrderTimer.Elapsed -= UpdateOrderTimer_Elapsed;
            }
        }

        #endregion //overrides methods

    }
}