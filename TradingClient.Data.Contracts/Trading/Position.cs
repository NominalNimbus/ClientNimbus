
namespace TradingClient.Data.Contracts
{
    public class Position
    {
        public string Symbol { get; private set; }
        public decimal Quantity { get; set; }
        public Side Side { get; set; }
        public decimal AvgOpenCost { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitPips { get; set; }
        public decimal CurrentPrice { get; set; }
        public string BrokerName { get; set; }
        public string AccountId { get; set; }
        public decimal Margin { get; set; }

        public Position(string symbol)
        {
            Symbol = symbol;
            BrokerName = string.Empty;
            AccountId = string.Empty;
        }

        public static string[] GetExportHeaders()
        {
            return new string[]
            {
                "Symbol",
                "Side",
                "Quantity",
                "Profit",
                "Total Open Cost",
                "Unfilled Long Cost",
                "Unfilled Short Cost"
            };
        }

        public object[] GetExportValues()
        {
            return new object[]
            {
                Symbol,
                Side.ToString(),
                Quantity,
                Profit,
                AvgOpenCost
            };
        }
    }
}
