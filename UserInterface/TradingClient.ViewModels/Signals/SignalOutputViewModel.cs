using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public class SignalOutputViewModel : AlertBaseViewModel<ScriptingLogData>, ISignalOutputViewModel
    {
        #region Ovverides

        protected override string ConvertItemToString(ScriptingLogData item) =>
          item.Time.ToString("HH:mm:ss") + " \t " + item.Message + Environment.NewLine;

        protected override ScriptingLogData CreateNewItem(string message) =>
            new ScriptingLogData(message, _scriptId, DateTime.Now);

        protected override ObservableCollection<ScriptingLogData> GetCollections() =>
             new ObservableCollection<ScriptingLogData>(Core.ScriptingLogManager.LogMessages.Where(i => i.SenderID == _scriptId));

        protected override void ClearItems() => Core.ScriptingLogManager.ClearLogMessages(_scriptId);

        public override string Title => _scriptId + " Outputs";

        #endregion //Ovverides

        public SignalOutputViewModel(IApplicationCore c, string scriptingId) :
            base(c, scriptingId)
        {

        }

    }
}
