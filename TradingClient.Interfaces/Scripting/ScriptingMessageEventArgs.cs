using System;

namespace TradingClient.Interfaces
{
    public class ScriptingMessageEventArgs : EventArgs
    {
        public string Message { get; }
        public string Writer { get; }
        public DateTime DateTime { get; }

        public ScriptingMessageEventArgs(string message)
        {
            Message = message;
        }

        public ScriptingMessageEventArgs(string message, string writer, DateTime dateTime)
            : this(message)
        {
            Writer = writer;
            DateTime = dateTime;
        }
    }
}