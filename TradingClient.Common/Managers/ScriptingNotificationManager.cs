using System;
using System.Collections.Generic;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;

namespace TradingClient.Common
{
    public class ScriptingNotificationManager : IScriptingNotificationManager
    {
        #region Properties and Events

        public List<NotificationItem> Notifications { get; }

        public event EventHandler<EventArgs<string, string>> NewNotification;

        #endregion //Properties and Events

        #region Constructors

        public ScriptingNotificationManager()
        {
            Notifications = new List<NotificationItem>();
        }

        #endregion // Constructors

        #region IScriptingNotificationManager

        public void ExecuteNotification(string sender, IEnumerable<string> messages)
        {
            if (string.IsNullOrWhiteSpace(sender))
                return;

            foreach (var message in messages)
            {
                if (string.IsNullOrEmpty(message))
                    continue;

                Notifications.Insert(0, new NotificationItem(sender, message));
                NewNotification?.Invoke(this, new EventArgs<string, string>(sender, message));
            }
        }

        public void ClearNotifications(string id)
        {
            if (string.IsNullOrEmpty(id))
                Notifications.Clear();
            else
                Notifications.RemoveAll(a => a.SenderID == id);
        }

        #endregion // IScriptingNotificationManager
    }
}
