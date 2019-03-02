using System;

namespace TradingClient.Data.Contracts
{
    public class NotificationItem
    {
        public string SenderID { get; }

        public string Message { get; }

        public DateTime Time { get; }

        public NotificationItem(string senderId, string message, DateTime time = default(DateTime))
        {
            Message = message;
            SenderID = senderId;
            Time = time == default(DateTime) ? DateTime.Now : time;
        }
    }
}