namespace TradingClient.Data.Contracts
{
    public class MarketLevel2
    {
        public int Level { get; set; }

        public decimal Bid { get; set; }

        public long BidSize { get; set; }

        public decimal Ask { get; set; }

        public long AskSize { get; set; }

        public decimal DailyLevel2AskSize { get; set; }

        public decimal DailyLevel2BidSize { get; set; }
    }
}