using System;

namespace TradingClient.ViewModelInterfaces
{
    public interface ILogItem
    {
        DateTime Date { get; }

        string Type { get; }

        string Text { get; }

        string Details { get; }
    }
}