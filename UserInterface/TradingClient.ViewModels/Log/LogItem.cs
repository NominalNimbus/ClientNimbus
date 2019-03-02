using System;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public class LogItem : ViewModelBase, ILogItem
    {
        public LogItem(DateTime date, string type, string text, string details)
        {
            Date = date;
            Type = type;
            Text = text;
            Details = details;
        }

        public DateTime Date { get; set; }

        public string Type { get; set; }

        public string Text { get; set; }

        public string Details { get; set; }

        public override string ToString() =>
            $"{Date} - {Type}  --> {Text}. {Details}";
    }
}