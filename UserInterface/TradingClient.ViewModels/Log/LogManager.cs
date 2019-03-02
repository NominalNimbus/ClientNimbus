using System;
using System.Collections.Generic;
using System.Globalization;

namespace TradingClient.ViewModels
{
    public static class LogManager
    {
        public static readonly List<LogItem> Items = new List<LogItem>();
        public static event Action<LogItem> OnNew;

        public static void ProcessLog(string date, string type, string text, string details)
        {
            var newItem = new LogItem(DateTime.Parse(date, CultureInfo.InvariantCulture), type, text, details);

            OnNew?.Invoke(newItem);
            Items.Insert(0, newItem);
        }
    }
}