using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bar = TradingClient.Data.Contracts.Bar;
using TradingClient.Data.Contracts;
using TradingClient.DataProvider.TradingService;

namespace TradingClient.DataProvider
{
    internal class HistoryRequest
    {
        public HistoryRequest()
        {
            Bars = new List<Bar>();
        }

        public int Interval { get; set; }

        public Timeframe Periodicity { get; set; }

        public int BarsCount { get; set; }

        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public int Level { get; set; }

        public bool? IncludeWeekendData { get; set; }

        public List<Bar> Bars { get; }

    }
}
