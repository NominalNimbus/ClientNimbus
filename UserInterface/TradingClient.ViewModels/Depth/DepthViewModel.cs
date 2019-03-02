using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Xml.Serialization;
using GalaSoft.MvvmLight.Command;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public class DepthViewModel : DocumentViewModel, IDepthViewModel
    {

        private readonly object _locker = new object();
        private int _selectedIndex;

        public override string Title => "Depth View";

        public override DocumentType DocumentType => DocumentType.DepthView;

        public ObservableCollection<IDepthViewItem> Items { get; set; }
        
        private IApplicationCore Core { get; }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex == value)
                    return;

                _selectedIndex = value;
                OnPropertyChanged("SelectedIndex");
            }
        }

        public ICommand CloseDepthTabCommand { get; private set; }

        public DepthViewModel(IApplicationCore core)
        {
            Core = core;
            Items = new ObservableCollection<IDepthViewItem>();
            Core.DataManager.OnNewTick += DataProviderOnNewQuote;
            CloseDepthTabCommand = new RelayCommand(ClosetabExecute, () => SelectedIndex >= 0);
        }

        private void ClosetabExecute()
        {
            if (SelectedIndex < 0 || SelectedIndex >= Items.Count)
                return;

            Core.DataManager.Unsubscribe(Items[SelectedIndex].Symbol, Items[SelectedIndex].DataFeed, this);

            Core.ViewFactory.Invoke(() =>
            {
                lock (_locker)
                    Items.RemoveAt(SelectedIndex);
            });           
        }

        public void AddNewItem(string symbol, string dataFeed)
        {
            var item = Items.FirstOrDefault(p => p.Symbol.Equals(symbol) && p.DataFeed.Equals(dataFeed));
            var prevIndex = -1;
            if (item != null && SelectedIndex > -1)
            {
                prevIndex = Items.IndexOf(item);
                Items.Remove(item);
            }

            var instrument = Core.DataManager.GetInstrumentFromDataFeed(symbol, dataFeed);

            if(instrument == null)
                return;

            Core.DataManager.Subscribe(symbol, dataFeed, this);
            var connectors = Core.DataManager.Broker.ActiveAccounts.Where(p => p.DataFeedName.Equals(dataFeed)).Select(p => p.BrokerName).Distinct().ToArray();

            var broker = "No broker adapters connected.";

            if (connectors.Length > 0)
                broker = string.Join(";", connectors);

            lock (_locker)
            {
                if (SelectedIndex > -1 && Items.Count > SelectedIndex)
                {
                    var index = SelectedIndex;
                    var i = Items[SelectedIndex];
                    if (index + 1 < Items.Count)
                        Items.Insert(index + 1,
                            new DepthViewItem {Symbol = symbol, DataFeed = dataFeed, Broker = broker});
                    else
                        Items.Add(new DepthViewItem {Symbol = symbol, DataFeed = dataFeed, Broker = broker});

                    Items.Remove(i);
                    if (prevIndex <= index)
                        Items.Insert(0, i);
                    else 
                        Items.Add(i);
                }
                else
                {
                    Items.Add(new DepthViewItem {Symbol = symbol, DataFeed = dataFeed, Broker = broker});

                    if (Items.Count == 1)
                        SelectedIndex = 0;
                }
            }
        }

        private void DataProviderOnNewQuote(TickData quote)
        {
            IDepthViewItem item;

            lock (_locker)
            {
                item = Items.FirstOrDefault(p => p.DataFeed.Equals(quote.DataFeed) && p.Symbol.Equals(quote.Symbol));

                if (item == null)
                    return;
            }

            for (var i = 0; i < quote.Level2.Count; i++)
            {
                if (item.Records.Count <= i)
                {
                    Core.ViewFactory.Invoke(() => {
                        item.Records.Add(new DepthViewRecord
                        {
                            Bid = quote.Level2[i].Bid,
                            Ask = quote.Level2[i].Ask,
                            BuyVolume = quote.Level2[i].AskSize,
                            SellVolume = quote.Level2[i].BidSize,
                            DailyVolume = quote.Level2[i].DailyLevel2AskSize + quote.Level2[i].DailyLevel2AskSize
                        });
                    });
                }
                else
                {
                    item.Records[i].Bid = quote.Level2[i].Bid;
                    item.Records[i].Ask = quote.Level2[i].Ask;
                    item.Records[i].BuyVolume = quote.Level2[i].AskSize;
                    item.Records[i].SellVolume = quote.Level2[i].BidSize;
                    item.Records[i].DailyVolume = quote.Level2[i].DailyLevel2AskSize + quote.Level2[i].DailyLevel2AskSize;
                }
            }

            if(item.Records.Count == 0)
                return;

            var highestBuy = item.Records.Max(p => p.BuyVolume);
            var highestSell = item.Records.Max(p => p.SellVolume);
            var highestOverall = item.Records.Max(p => p.DailyVolume);
            if (highestOverall == 0)
                highestOverall = 1;

            foreach (var record in item.Records)
            {
                record.BuyScale = record.BuyVolume/highestBuy*100;
                record.SellScale = record.SellVolume/highestSell*100;
                record.DailyScale = (decimal)(record.DailyVolume / highestOverall * 100);
            }
        }
        
        public override void LoadWorkspaceData(byte[] data)
        {
            try
            {
                List<DepthViewWorkspace> workspace;
                using (var stream = new MemoryStream(data))
                {
                    var serializer = new XmlSerializer(typeof(List<DepthViewWorkspace>), new[] { typeof(string) });
                    workspace = (List<DepthViewWorkspace>)serializer.Deserialize(stream);
                }

                Core.ViewFactory.Invoke(() =>
                {
                    lock (Items)
                        Items.Clear();

                    foreach (var depthViewWorkspace in workspace)
                        AddNewItem(depthViewWorkspace.Symbol, depthViewWorkspace.DataFeed);
                });
            }
            catch { }
        }
        
        public override byte[] SaveWorkspaceData()
        {
            using (var stream = new MemoryStream())
            {
                var serializer = new XmlSerializer(typeof(List<DepthViewWorkspace>), new[] { typeof(string) });
                serializer.Serialize(stream, Items.Select(p => new DepthViewWorkspace
                {
                     DataFeed = p.DataFeed,
                     Symbol = p.Symbol
                }).ToList());
                return stream.GetBuffer();
            }
        }
    }

    [Serializable]
    public class DepthViewWorkspace
    {
        public string Symbol { get; set; }
        public string DataFeed { get; set; }
    }
}
