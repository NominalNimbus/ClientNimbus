using System;
using System.Diagnostics;

namespace TradingClient.Data.Contracts
{
    [Serializable]
    public class Security
    {
        public int Id { get; set; }

        public string Symbol { get; set; }

        public string Name { get; set; }

        public string DataFeed { get; set; }

        public decimal PriceIncrement { get; set; }

        public decimal QtyIncrement { get; set; }

        public decimal ContractSize { get; set; }

        public decimal Point { get; set; }

        public decimal MarginRate { get; set; }

        public int Digits { get; set; }

        public string BaseCurrency { get; set; }

        public string UnitOfMeasure { get; set; }

        public string AssetClass { get; set; }
        
        public decimal MaxPosition { get; set; }

        public decimal UnitPrice { get; set; }

        public TimeSpan MarketOpen { get; set; }

        public TimeSpan MarketClose { get; set; }
    }
}