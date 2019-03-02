using System;
using System.Collections.Generic;

namespace TradingClient.Data.Contracts
{
    public class ScriptingReceivedEventArgs : EventArgs
    {
        public Dictionary<string, List<ScriptingParameterBase>> Indicators { get; private set; }
        public List<Signal> Signals { get; private set; }
        public List<string> DefaultIndicators { get; private set; }

        public ScriptingReceivedEventArgs(Dictionary<string, List<ScriptingParameterBase>> indicators, List<Signal> signals, List<string> defaultIndicators)
        {
            Indicators = new Dictionary<string, List<ScriptingParameterBase>>(indicators);
            Signals = new List<Signal>(signals);
            DefaultIndicators = new List<string>(defaultIndicators);
        }
    }
}
