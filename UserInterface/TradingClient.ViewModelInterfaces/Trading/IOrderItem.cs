using System;
using TradingClient.Data.Contracts;

namespace TradingClient.ViewModelInterfaces
{
    public interface IOrderItem : ITradeItem
    {
        Order Order { get; }

        decimal SL { get; set;  }

        decimal TP { get; set; }

        decimal FilledQty { get; set; }
    }
}