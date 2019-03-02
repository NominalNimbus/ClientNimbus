using System;
using System.Collections.Generic;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;


namespace TradingClient.Common
{
    public class ScriptingLogManager : IScriptingLogManager
    {
        #region Constructor

        public ScriptingLogManager()
        {
            LogMessages = new List<ScriptingLogData>();
        }

        #endregion //Constructor

        #region IScriptingLogManager

        public List<ScriptingLogData> LogMessages { get; }

        public event EventHandler<ScriptingMessageEventArgs> OnNewLogMessage;

        public void SendLogMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            AppLogger.Info(message);
            LogMessages.Insert(0, new ScriptingLogData(message, string.Empty, DateTime.Now));
            OnNewLogMessage?.Invoke(this, new ScriptingMessageEventArgs(message));
        }

        public void SendLogMessage(ScriptingLogData data)
        {
            if (data == null)
                return;

            AppLogger.Info(data.Message);
            LogMessages.Insert(0, new ScriptingLogData(data.Message, data.SenderID, data.Time));
            OnNewLogMessage?.Invoke(this, new ScriptingMessageEventArgs(data.Message, data.SenderID, data.Time));
        }

        public void ClearLogMessages(string id)
        {
            if (string.IsNullOrEmpty(id))
                LogMessages.Clear();
            else
                LogMessages.RemoveAll(a => a.SenderID == id);
        }


        #endregion // IScriptingLogManager
    }
}