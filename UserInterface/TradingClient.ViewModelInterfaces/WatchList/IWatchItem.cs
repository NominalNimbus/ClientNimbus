using System;
using TradingClient.Data.Contracts;

namespace TradingClient.ViewModelInterfaces
{
    public interface IWatchItem
    {
        #region Properties

        string Symbol { get; set; }

        string DataFeed { get; set; }

        int Digits { get; set; }

        DateTime Date { get; set; }

        decimal Price { get; set; }

        decimal Open { get; set; }

        decimal High { get; set; }

        decimal Low { get; set; }

        long Volume { get; set; }

        decimal Bid { get; set; }

        decimal BidChange { get; set; }

        decimal BidSize { get; set; }

        decimal Ask { get; set; }

        decimal AskChange { get; set; }

        decimal AskSize { get; set; }

        #endregion //Properties

        #region Methods

        void UpdateTick(TickData tick);

        void UpdateBar(Bar bar);

        void Clear();

        bool EqualsSymbol(string symbol, string dataFeed);

        #endregion //Methods
    }
}