using System;
using System.Diagnostics;
using TradingClient.Common;
using TradingClient.Data.Contracts;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public class WatchItem : Observable, IWatchItem
    {

        #region Members

        private string _symbol;
        private string _dataFeed;
        private int _digits;
        private DateTime _date;
        private decimal _price;
        private decimal _open;
        private decimal _high;
        private decimal _low;
        private long _volume;
        private decimal _bid;
        private decimal _bidChange;
        private decimal _bidSize;
        private decimal _ask;
        private decimal _askChange;
        private decimal _askSize;

        #endregion //Members

        public WatchItem()
        {
            Symbol = string.Empty;
            DataFeed = string.Empty;
            Open = High = Low = Bid = Ask = BidChange = AskChange = Volume = Digits = 0;
        }

        #region Properties

        public string Symbol
        {
            get => _symbol;
            set => SetPropertyValue(ref _symbol, value, nameof(Symbol));
        }

        public string DataFeed
        {
            get => _dataFeed;
            set => SetPropertyValue(ref _dataFeed, value, nameof(DataFeed));
        }

        public int Digits
        {
            get => _digits;
            set => SetPropertyValue(ref _digits, value, nameof(Digits));
        }

        public DateTime Date
        {
            get => _date;
            set => SetPropertyValue(ref _date, value, nameof(Date));
        }

        public decimal Price
        {
            get => _price;
            set => SetPropertyValue(ref _price, value, nameof(Price));
        }

        public decimal Open
        {
            get => _open;
            set => SetPropertyValue(ref _open, value, nameof(Open));
        }

        public decimal High
        {
            get => _high;
            set => SetPropertyValue(ref _high, value, nameof(High));
        }

        public decimal Low
        {
            get => _low;
            set => SetPropertyValue(ref _low, value, nameof(Low));
        }

        public long Volume
        {
            get => _volume;
            set => SetPropertyValue(ref _volume, value, nameof(Volume));
        }

        public decimal Bid
        {
            get => _bid;
            set => SetPropertyValue(ref _bid, value, nameof(Bid));
        }

        public decimal BidChange
        {
            get => _bidChange;
            set => SetPropertyValue(ref _bidChange, value, nameof(BidChange));
        }

        public decimal BidSize
        {
            get => _bidSize;
            set => SetPropertyValue(ref _bidSize, value, nameof(BidSize));
        }

        public decimal Ask
        {
            get => _ask;
            set => SetPropertyValue(ref _ask, value, nameof(Ask));
        }

        public decimal AskChange
        {
            get => _askChange;
            set => SetPropertyValue(ref _askChange, value, nameof(AskChange));
        }

        public decimal AskSize
        {
            get => _askSize;
            set => SetPropertyValue(ref _askSize, value, nameof(AskSize));
        }

        #endregion //Properties

        #region Public Methods

        public void UpdateTick(TickData tick)
        {
            if (Bid > 0 && tick.Bid > 0 && Bid != tick.Bid)
                BidChange = Round(tick.Bid - Bid);

            if (Ask > 0 && tick.Ask > 0 && Ask != tick.Ask)
                AskChange = Round(tick.Ask - Ask);

            Price = Round(tick.LastPrice);
            if (Open == 0)
                Open = Price;

            if (High < Price || High == 0)
                High = Price;

            if (Low > Price || Low == 0)
                Low = Price;

            Volume = tick.Volume;
            Date = tick.Time;
            Bid = Round(tick.Bid);
            BidSize = tick.BidSize;
            Ask = Round(tick.Ask);
            AskSize = tick.AskSize;
        }

        public void UpdateBar(Bar bar)
        {
            Open = bar.Open;
            High = bar.High;
            Low = bar.Low;
        }

        public bool EqualsSymbol(string symbol, string dataFeed) =>
            Symbol.EqualsValue(symbol) && DataFeed.EqualsValue(dataFeed);

        public void Clear()
        {
            Price = 0;
            Bid = 0;
            Ask = 0;
            Date = DateTime.MinValue;
            BidChange = 0;
            AskChange = 0;
            Volume = 0;
            AskSize = 0;
            BidSize = 0;
            Open = 0;
            High = 0;
            Low = 0;
        }

        #endregion //Public Methods

        #region Protected

        private decimal Round(decimal value) =>
            Math.Round(value, Digits);

        #endregion //Protected
    }
}