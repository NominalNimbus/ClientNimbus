using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using NLog;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    internal class CombinedPositionsViewModel : DocumentViewModel, ICombinedPositionsViewModel
    {
        private static readonly Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly object _locker = new object();
        private bool _isTradingAllowed;
        private AccountInfo _selectedAccount;
        private IPositionItem _selectedPosition;

        public ICommand ExportCommand { get; private set; }
        public ICommand ClosePositionCommand { get; private set; }

        #region Properties

        private IApplicationCore Core { get; }

        public override string Title => "Combined Positions";

        public override DocumentType DocumentType => DocumentType.CombinedPositions;

        public IList<IPositionItem> Positions { get; private set; }

        public bool IsTradingAllowed
        {
            get => _isTradingAllowed;
            set
            {
                if (value.Equals(_isTradingAllowed))
                    return;
                _isTradingAllowed = value;
                UpdatePositions();
                OnPropertyChanged("IsTradingAllowed");
            }
        }

        public AccountInfo SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                if (Equals(value, _selectedAccount))
                    return;
                _selectedAccount = value;
                UpdatePositions();
                OnPropertyChanged("SelectedAccount");
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

        public  ObservableCollection<AccountInfo> Accounts { get; private set; }

        #endregion

        public CombinedPositionsViewModel(IApplicationCore core)
        {
            Core = core;
            Positions = new List<IPositionItem>();
            Accounts =Core.DataManager.Broker.ActiveAccounts;

            if (Core.DataManager.Broker.DefaultAccount != null)
                SelectedAccount = Core.DataManager.Broker.DefaultAccount;
            else if (Accounts.Count > 0)
                SelectedAccount = Accounts.First();

            lock (core)
            {
                Positions = new ObservableCollection<IPositionItem>(Core.DataManager.Broker.Positions
                    .Where(p => SelectedAccount != null && p.AccountId.Equals(SelectedAccount.ID)).Select(position =>
                {
                    var instrument = Core.DataManager.GetInstrumentFromBroker(position.Symbol, SelectedAccount.BrokerName);
                    return new PositionItem(position)
                    {
                        CurrentPrice = position.CurrentPrice,
                        CurrentPriceChange = 0,
                        Profit = position.Profit,
                        ProfitPips = position.ProfitPips,
                        Side = position.Side,
                        Instrument = instrument,
                        Qty = Math.Abs(position.Quantity)
                    };
                }));
            }
            IsTradingAllowed = Core.DataManager.Broker.IsActive;
            Core.DataManager.Broker.PositionsChanged += BrokerOnPositionChanged;
            Core.DataManager.Broker.PositionUpdated += BrokerOnPositionUpdated;
            Core.DataManager.Broker.OnAccountStateChanged += BrokerOnOnAccountStateChanged;
            Core.DataManager.Broker.PropertyChanged += BrokerOnPropertyChanged;

            ExportCommand = new RelayCommand(ExportExecute);
            ClosePositionCommand = new RelayCommand(ClosePositionExecute, () => SelectedPosition != null && SelectedAccount != null 
                                                                                                         && SelectedPosition.Position.Quantity != 0);
        }

        private void BrokerOnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName.Equals("DefaultAccount", StringComparison.OrdinalIgnoreCase))
            {
                if (sender is IBrokerManager broker)
                    SelectedAccount = broker.DefaultAccount;
            }
        }

        private void ClosePositionExecute()
        {
            if(SelectedPosition != null && SelectedAccount != null)
                Core.DataManager.Broker.ClosePosition(SelectedPosition.Position.Symbol, SelectedAccount.ID);
        }

        private void BrokerOnPositionChanged(object sender, EventArgs args)
        {
            UpdatePositions();
        }

        private void UpdatePositions()
        {
            var positions = new List<IPositionItem>(Core.DataManager.Broker.Positions
                .Where(p => SelectedAccount != null && p.AccountId.Equals(SelectedAccount.ID))
                .Select(position =>
            {
                var instrument = Core.DataManager.GetInstrumentFromBroker(position.Symbol, position.BrokerName);

                return new PositionItem(position)
                {
                    CurrentPrice = position.CurrentPrice,
                    CurrentPriceChange = 0,
                    Side = position.Side,
                    Profit = position.Profit,
                    ProfitPips = position.ProfitPips,
                    Instrument = instrument,
                    AvgPrice = position.AvgOpenCost,
                    Qty = Math.Abs(position.Quantity)
                };
            }));

            Core.ViewFactory.Invoke(() =>
            {
                lock (_locker)
                {
                    var prevSelSymbol = SelectedPosition?.Instrument?.Symbol;
                    Positions.Clear();
                    foreach (var order in positions)
                        Positions.Add(order);
                    if (prevSelSymbol != null && Positions.Count > 0)
                        SelectedPosition = Positions.FirstOrDefault(i => i.Instrument.Symbol == prevSelSymbol);
                }
            });
        }

        private void BrokerOnPositionUpdated(object sender, EventArgs<Position> eventArgs)
        {
            Core.ViewFactory.Invoke(() =>
            {
                lock (_locker)
                {
                    var pos = Positions.FirstOrDefault(p => p.Position.Symbol.Equals(eventArgs.Value.Symbol) 
                        && p.Position.AccountId == eventArgs.Value.AccountId);
                    if (pos != null)
                    {
                        pos.CurrentPriceChange = eventArgs.Value.CurrentPrice - pos.CurrentPrice;
                        pos.CurrentPrice = eventArgs.Value.CurrentPrice;
                        pos.Profit = eventArgs.Value.Profit;
                        pos.Side = eventArgs.Value.Side;
                        pos.ProfitPips = eventArgs.Value.ProfitPips;
                        pos.Qty = Math.Abs(eventArgs.Value.Quantity);
                        pos.AvgPrice = eventArgs.Value.AvgOpenCost;
                        pos.Margin = eventArgs.Value.Margin;
                    }
                }
            });
        }

        private void BrokerOnOnAccountStateChanged(object sender, EventArgs eventArgs)
        {
            IsTradingAllowed = Core.DataManager.Broker.IsActive;
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
                    ExcelExportManager.ExportPositions(file, 
                        Positions.Select(model => model.Position).ToList());
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "XLSX export failure");
                    Core.ViewFactory.ShowMessage("Failed to export to XLSX: " + ex.Message, 
                        "Error", MsgBoxButton.OK, MsgBoxIcon.Error);
                }
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                Core.DataManager.Broker.PositionsChanged -= BrokerOnPositionChanged;
                Core.DataManager.Broker.PositionUpdated -= BrokerOnPositionUpdated;
                Core.DataManager.Broker.OnAccountStateChanged -= BrokerOnOnAccountStateChanged;
                Core.DataManager.Broker.PropertyChanged -= BrokerOnPropertyChanged;
            }
        }
    }
}