using System;
using System.Collections.Generic;
using TradingClient.Data.Contracts;

namespace TradingClient.ViewModels
{
    [Serializable]
    public class WatchListWorkspace
    {
        public List<Security> Instrument { get; set; }

        public bool DateCol { get; set; }

        public bool PriceCol { get; set; }

        public bool BidCol { get; set; }

        public bool BidSizeCol { get; set; }

        public bool AskCol { get; set; }

        public bool AskSizeCol { get; set; }

        public bool OpenCol { get; set; }

        public bool HighCol { get; set; }

        public bool LowCol { get; set; }

        public WatchListWorkspace()
        {
            DateCol = true;
            PriceCol = true;
            BidCol = true;
            AskCol = true;
            BidSizeCol = true;
            AskSizeCol = true;
            OpenCol = true;
            HighCol = true;
            LowCol = true;

            Instrument = new List<Security>();
        }
    }
}
