using System;
using System.Collections.Generic;
using TradingClient.Data.Contracts;

namespace TradingClient.Interfaces
{
    public interface IScriptingLogManager
    {
        List<ScriptingLogData> LogMessages { get; }

        event EventHandler<ScriptingMessageEventArgs> OnNewLogMessage;

        void SendLogMessage(string message);
        void SendLogMessage(ScriptingLogData data);

        void ClearLogMessages(string id);
    }
}