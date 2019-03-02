using System;
using System.Collections.Generic;

namespace TradingClient.Data.Contracts
{
    public class ScriptingSavedEventArgs : EventArgs
    {
        public List<ScriptingParameterBase> Parameters { get; private set; }
        public string Name { get; private set; }
        public ScriptingType ScriptingType { get; private set; }

        public ScriptingSavedEventArgs(IEnumerable<ScriptingParameterBase> parameters, string name, ScriptingType scriptingType)
        {
            Parameters = new List<ScriptingParameterBase>(parameters);
            Name = name;
            ScriptingType = scriptingType;
        }
    }
}