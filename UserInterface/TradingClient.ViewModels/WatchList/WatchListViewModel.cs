using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Xml.Serialization;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using NLog;
using TradingClient.Common;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    [Serializable]
    public class WatchListViewModel : DocumentViewModel, IWatchListViewModel
    {
        #region Members

        private const string SymbolListDialogFilter = "Watch List (*.wl)|*.wl";
        private readonly List<Tuple<string, string, string>> _historyRequests;
        private readonly List<Security> _subscribedSymbols;
        private IWatchItem _selectedItem;

        private bool _dateCol;
        private bool _priceCol;
        private bool _bidCol;
        private bool _bidSizeCol;
        private bool _askCol;
        private bool _askSizeCol;
        private bool _openCol;
        private bool _highCol;
        private bool _lowCol;

        #endregion Members

        public WatchListViewModel(IApplicationCore core)
        {
            Core = core;
            _historyRequests = new List<Tuple<string, string, string>>();

            DataFeeds = new ObservableCollection<string>(Core.DataManager.DatafeedList);
            _subscribedSymbols = new List<Security>();
            Items = new ObservableCollection<IWatchItem>();
            Items.CollectionChanged += ItemsCollectionChanged;

            PlaceOrderCommand = new RelayCommand(PlaceOrderCommandExecute, () => IsItemSelected && Core.DataManager.Broker.IsActive);
            AddToDepthViewCommand = new RelayCommand(AddToDepthViewCommandExecute, IsItemSelected);
            RemoveSelectedCommand = new RelayCommand(RemoveSelectedCommandExecute, IsItemSelected);
            RemoveCurrentCommand = new RelayCommand<IWatchItem>(RemoveCurrentCommandExecute);
            ExportCommand = new RelayCommand(ExportCommandExecute);
            ImportCommand = new RelayCommand(ImportCommandExecute);

            core.DataManager.OnNewTick += OnNewQuote;
            core.DataManager.OnHistoryDataReady += OnHistoricalData;
            EnsureAddEmptyItem();

            DateCol = true;
            BidCol = true;
            AskCol = true;
            BidSizeCol = false;
            AskSizeCol = false;
            PriceCol = true;
            OpenCol = false;
            HighCol = false;
            LowCol = false;
        }

        #region Properties

        private IApplicationCore Core { get; }

        public override string Title => "Watch List";

        public override DocumentType DocumentType => DocumentType.WatchList;

        public ObservableCollection<IWatchItem> Items { get; set; }

        public ObservableCollection<string> DataFeeds { get; private set; }

        public IWatchItem SelectedItem
        {
            get => _selectedItem;
            set => SetPropertyValue(ref _selectedItem, value, nameof(SelectedItem));
        }

        private bool IsItemSelected =>
            SelectedItem?.Symbol != null;
        
        public bool DateCol
        {
            get => _dateCol;
            set => SetPropertyValue(ref _dateCol, value, nameof(DateCol));
        }

        public bool PriceCol
        {
            get => _priceCol;
            set => SetPropertyValue(ref _priceCol, value, nameof(PriceCol));
        }

        public bool BidCol
        {
            get => _bidCol;
            set => SetPropertyValue(ref _bidCol, value, nameof(BidCol));
        }

        public bool BidSizeCol
        {
            get => _bidSizeCol;
            set => SetPropertyValue(ref _bidSizeCol, value, nameof(BidSizeCol));
        }

        public bool AskCol
        {
            get => _askCol;
            set => SetPropertyValue(ref _askCol, value, nameof(AskCol));
        }

        public bool AskSizeCol
        {
            get => _askSizeCol;
            set => SetPropertyValue(ref _askSizeCol, value, nameof(AskSizeCol));
        }

        public bool OpenCol
        {
            get => _openCol;
            set => SetPropertyValue(ref _openCol, value, nameof(OpenCol));
        }

        public bool HighCol
        {
            get => _highCol;
            set => SetPropertyValue(ref _highCol, value, nameof(HighCol));
        }

        public bool LowCol
        {
            get => _lowCol;
            set => SetPropertyValue(ref _lowCol, value, nameof(LowCol));
        }

        #endregion //Properties

        #region Commands

        public ICommand PlaceOrderCommand { get; private set; }

        public ICommand AddToDepthViewCommand { get; private set; }

        public ICommand RemoveSelectedCommand { get; private set; }

        public RelayCommand<IWatchItem> RemoveCurrentCommand { get; private set; }

        public ICommand ExportCommand { get; private set; }

        public ICommand ImportCommand { get; private set; }

        #endregion //Commands

        #region Command Execution

        private void RemoveSelectedCommandExecute()
        {
            RemoveCurrentCommandExecute(SelectedItem);
            SelectedItem = null;
        }

        private void RemoveCurrentCommandExecute(IWatchItem removeItem)
        {
            if (removeItem == null)
                return;

            lock (Items)
            {
                Items.Remove(removeItem);
            }
            if (removeItem is ViewModelBase item)
                item.PropertyChanged -= ItemOnPropertyChanged;

            EnsureAddEmptyItem();
        }

        private void PlaceOrderCommandExecute()
        {
            var instrument = GetInstrumentBySymbol(SelectedItem.Symbol, SelectedItem.DataFeed);
            if (instrument != null)
            {
                var order = new Order("", instrument.Symbol, 1, OrderType.Market, Side.Buy, TimeInForce.GoodTilCancelled);
                using (var placeOrderVm = new PlaceOrderViewModel(Core, order, SelectedItem.DataFeed))
                    Core.ViewFactory.ShowDialogView(placeOrderVm);
            }
        }

        private void AddToDepthViewCommandExecute()
        {
            if (SelectedItem == null)
                return;

            Core.ViewFactory.AddSymbol2Depth(SelectedItem.Symbol, SelectedItem.DataFeed);
        }
        
        private void ImportCommandExecute()
        {
            var file = Core.ViewFactory.ShowOpenFileDialog(SymbolListDialogFilter, Core.PathManager.SymbolListDirectory);

            if (string.IsNullOrEmpty(file))
                return;

            try
            {
                using (var stream = File.Open(file, FileMode.Open, FileAccess.Read))
                {
                    var serializer = new XmlSerializer(typeof(List<Security>));
                    var list = (List<Security>)serializer.Deserialize(stream);
                    AppendInstruments(list);
                }
            }
            catch (Exception ex)
            {
                Core.ViewFactory.ShowMessage("Import error: " + ex.Message, "Error", MsgBoxButton.OK, MsgBoxIcon.Error);
            }

            if (!Items.Any(p => string.IsNullOrEmpty(p.Symbol)))
                EnsureAddEmptyItem();
        }

        private void ExportCommandExecute()
        {
            var file = Core.ViewFactory.ShowSaveFileDialog(SymbolListDialogFilter, Core.PathManager.SymbolListDirectory);

            if (string.IsNullOrEmpty(file))
                return;

            try
            {
                var list = Items.Select(p => GetInstrumentBySymbol(p.Symbol, p.DataFeed)).ToList();

                var serializer = new XmlSerializer(typeof(List<Security>));

                using (var stream = File.Open(file, FileMode.Create, FileAccess.Write))
                    serializer.Serialize(stream, list);
            }
            catch (Exception ex)
            {
                Core.ViewFactory.ShowMessage("Export error: " + ex.Message,
                    "Error", MsgBoxButton.OK, MsgBoxIcon.Error);
                return;
            }

            Core.ViewFactory.ShowMessage("Watch list has been saved",
                "Information", MsgBoxButton.OK, MsgBoxIcon.Information);
        }

        #endregion //Command Execution

        #region Overrides

        public override void LoadWorkspaceData(byte[] data)
        {
            try
            {
                WatchListWorkspace workspace;
                using (var stream = new MemoryStream(data))
                {
                    var serializer = new XmlSerializer(typeof(WatchListWorkspace), new[] {typeof(string), typeof(bool)});
                    workspace = (WatchListWorkspace)serializer.Deserialize(stream);
                }
                
                if (workspace != null)
                {
                    AppendInstruments(workspace.Instrument);

                    AskCol = workspace.AskCol;
                    BidCol = workspace.BidCol;
                    AskSizeCol = workspace.AskSizeCol;
                    BidSizeCol = workspace.BidSizeCol;
                    DateCol = workspace.DateCol;
                    PriceCol = workspace.PriceCol;
                    HighCol = workspace.HighCol;
                    LowCol = workspace.LowCol;
                    OpenCol = workspace.OpenCol;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Load Workspace error");
            }

            if (!Items.Any(p => string.IsNullOrEmpty(p.Symbol)))
                EnsureAddEmptyItem();
        }
        
        public override byte[] SaveWorkspaceData()
        {
            using (var stream = new MemoryStream())
            {
                var serializer = new XmlSerializer(typeof(WatchListWorkspace), new[] { typeof(string), typeof(bool) });
                serializer.Serialize(stream, new WatchListWorkspace
                {
                    Instrument = new List<Security>(Items.Select(p => GetInstrumentBySymbol(p.Symbol, p.DataFeed))),
                    AskCol = AskCol,
                    BidCol = BidCol,
                    AskSizeCol = AskSizeCol,
                    BidSizeCol = BidSizeCol,
                    DateCol = DateCol,
                    PriceCol = PriceCol,
                    HighCol = HighCol,
                    LowCol = LowCol,
                    OpenCol = OpenCol
                });
                return stream.GetBuffer();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                ClearItems();
        }

        #endregion //Overrides

        #region DataManager methods

        private void OnNewQuote(TickData tick)
        {
            List<IWatchItem> updateItems;
            lock (Items)
                updateItems = Items.Where(itm => itm.EqualsSymbol(tick.Symbol, tick.DataFeed)).ToList();

            updateItems.ForEach(itm => itm.UpdateTick(tick));
        }

        private void OnHistoricalData(string s, IEnumerable<Bar> bars)
        {
            Tuple<string, string, string> request = null;
            lock (_historyRequests)
            {
                request = _historyRequests.FirstOrDefault(r => r.Item1 == s);
                if (request != null)
                    _historyRequests.Remove(request);
            }
            var lastBar = bars.LastOrDefault();

            if (request == null || lastBar == null)
                return;

            List<IWatchItem> updateItems;
            lock (Items)
                updateItems = Items.Where(itm => itm.EqualsSymbol(request.Item2, request.Item3)).ToList();

            updateItems.ForEach(itm => itm.UpdateBar(lastBar));
        }

        private void Subscribe(string symbol, string df)
        {
            var instrument = GetInstrumentBySymbol(symbol.ToUpper(), df);
            if (instrument == null)
                return;

            var requestId = $"{symbol}_{df}_{Id}";

            lock(_historyRequests)
                _historyRequests.Add(new Tuple<string, string, string>(requestId, symbol, df));

            Core.DataManager.GetHistory(requestId, symbol, df, TimeFrame.Day, 1, 
                DateTime.MinValue, DateTime.MinValue, 5, 0, null);

            if (!_subscribedSymbols.Any(p => p.DataFeed.Equals(instrument.DataFeed) && p.Symbol.Equals(instrument.Symbol)))
            {
                _subscribedSymbols.Add(instrument);
                Core.DataManager.Subscribe(instrument.Symbol, instrument.DataFeed, this);
            }
        }

        #endregion //DataManager methods

        #region Helper methods

        private Security GetInstrumentBySymbol(string symbol, string df)
        {
            try
            {
                return string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(df) ?
                    null :
                    Core.DataManager.GetInstrumentFromDataFeed(symbol, df);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void EnsureAddEmptyItem()
        {
            if (DataFeeds.Count == 0 || ContainsEmptyItem())
                return;

            Core.ViewFactory.BeginInvoke(AddEmptyItem);
        }

        private bool ContainsEmptyItem()
        {
            lock (Items)
            {
                return Items.Any(p => string.IsNullOrEmpty(p.Symbol));
            }
        }

        private void AddEmptyItem()
        {
            lock (Items)
            {
                Items.Add(new WatchItem
                {
                    Symbol = string.Empty,
                    DataFeed = Items.LastOrDefault()?.DataFeed ?? DataFeeds.First()
                });
            }
        }

        private void ClearItems()
        {
            lock (Items)
            {
                foreach (var instrument in _subscribedSymbols)
                {
                    Core.DataManager.Unsubscribe(instrument.Symbol, instrument.DataFeed, this);
                }
                _subscribedSymbols.Clear();
                Items.Clear();
            }
        }
        
        private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Add && args.NewItems.Count > 0)
            {
                foreach (var item in args.NewItems)
                {
                    if (args.NewItems[0] is Observable watchItem)
                        watchItem.PropertyChanged += ItemOnPropertyChanged;
                }
            }
        }

        private void ItemOnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName.Equals("Symbol") || args.PropertyName.Equals("DataFeed"))
            {
                if (sender is IWatchItem item && !string.IsNullOrEmpty(item.Symbol) && !string.IsNullOrEmpty(item.DataFeed))
                {
                    var instrument = GetInstrumentBySymbol(item.Symbol, item.DataFeed);
                    if (instrument == null)
                    {
                        item.Clear();
                    }
                    else
                    {
                        item.Digits = instrument.Digits;
                    }

                    if (Items.Count(p => p.Symbol.EqualsValue(item.Symbol) && item.DataFeed.Equals(p.DataFeed)) > 1)
                    {
                        Core.ViewFactory.ShowMessage($"Symbol '{item.Symbol}' and datafeed '{item.DataFeed}' already exist.");
                        RemoveCurrentCommandExecute(item);
                        return;
                    }

                    if (!Items.Any(p => string.IsNullOrEmpty(p.Symbol)))
                        EnsureAddEmptyItem();

                    Subscribe(item.Symbol, item.DataFeed);
                }
            }
        }

        private void AppendInstruments(IEnumerable<Security> instruments)
        {
            ClearItems();
            foreach (var instrument in instruments)
            {
                if (instrument == null || !Core.DataManager.DatafeedList.Contains(instrument.DataFeed) ||
                    string.IsNullOrEmpty(instrument.Symbol) || string.IsNullOrEmpty(instrument.DataFeed))
                    continue;

                var item = new WatchItem
                {
                    Symbol = instrument.Symbol,
                    DataFeed = instrument.DataFeed,
                    Digits = instrument.Digits
                };

                lock (Items)
                    Items.Add(item);

                var tick = Core.DataManager.GetTick(instrument.Symbol, instrument.DataFeed);
                if (tick != null)
                    item.UpdateTick(tick);

                Subscribe(instrument.Symbol, instrument.DataFeed);
            }
        }

        #endregion //Helper methods
    }
}