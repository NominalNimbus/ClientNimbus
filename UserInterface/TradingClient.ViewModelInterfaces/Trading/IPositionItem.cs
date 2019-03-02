using System;
using TradingClient.Data.Contracts;

namespace TradingClient.ViewModelInterfaces
{
    public interface IPositionItem : ITradeItem
    {
        Position Position { get; set; }

        Side Side { get; set; }

        decimal AvgPrice { get; set; }

        decimal Qty { get; set; }

        decimal Margin { get; set; }
    }
}