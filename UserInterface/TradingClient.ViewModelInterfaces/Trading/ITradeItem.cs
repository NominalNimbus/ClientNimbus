using TradingClient.Data.Contracts;

namespace TradingClient.ViewModelInterfaces
{
    public interface ITradeItem
    {
        Security Instrument { get; set; }

        decimal CurrentPrice { get; set; }

        decimal CurrentPriceChange { get; set; }

        decimal Profit { get; set; }

        decimal ProfitPips { get; set; }

        bool IsServerSide { get; set; }
    }
}