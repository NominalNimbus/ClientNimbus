using System;
using System.Collections.Generic;

namespace TradingClient.Data.Contracts
{
    public class ScriptingLogEventArgs : EventArgs
    {
        public List<ScriptingLogData> Value { get; set; }
    }

    public class ScriptingLogData : NotificationItem
    {
        public ScriptingLogData(string writer, string message, DateTime dateTime):
            base(message, writer, dateTime)
        {

        }
    }
}
