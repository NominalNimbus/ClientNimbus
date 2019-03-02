using System;
using System.Collections.Generic;

using TradingClient.Data.Contracts;

namespace TradingClient.Interfaces
{
    public interface IScriptingNotificationManager
    {
        List<NotificationItem> Notifications { get; }

        event EventHandler<EventArgs<string, string>> NewNotification;

        void ExecuteNotification(string sender, IEnumerable<string> messages);

        void ClearNotifications(string id);
    }
}