using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TradingClient.Data.Contracts
{
    public class TickData : ICloneable
    {
        public TickData()
        {
           Level2 = new List<MarketLevel2>();
        }

        #region Properties

        public string Symbol { get; set; }

        public string DataFeed { get; set; }

        public DateTime Time { get; set; }

        public decimal LastPrice { get; set; } 

        public decimal Ask { get; set; }

        public decimal AskSize { get; set; } 

        public decimal Bid { get; set; }

        public decimal BidSize { get; set; }

        public long Volume { get; set; }

        public List<MarketLevel2> Level2 { get; set; }

        #endregion //Properties

        public object Clone()
        {
            var cloneObject = (TickData)MemberwiseClone();
            cloneObject.Level2 = Level2.ToList();
            return cloneObject;
        }
    }
}