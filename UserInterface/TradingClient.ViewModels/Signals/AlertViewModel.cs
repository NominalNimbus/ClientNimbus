using System;
using System.Collections.ObjectModel;
using System.Linq;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public class AlertViewModel : AlertBaseViewModel<NotificationItem>, IAlertViewModel
    {
        #region Ovverides

        protected override NotificationItem CreateNewItem(string message) => new NotificationItem(_scriptId, message);

        protected override ObservableCollection<NotificationItem> GetCollections() =>
             new ObservableCollection<NotificationItem>(Core.ScriptingNotificationManager.Notifications.Where(i => i.SenderID == _scriptId));

        protected override string ConvertItemToString(NotificationItem item) =>
            item.Time.ToString("HH:mm:ss") + " \t " + item.Message + Environment.NewLine;

        protected override void ClearItems() => Core.ScriptingNotificationManager.ClearNotifications(_scriptId);

        public override string Title => _scriptId + " Alerts";
        
        #endregion //Ovverides

        public AlertViewModel(IApplicationCore c, string scriptingID) :
            base(c, scriptingID)
        {

        }
    }
}