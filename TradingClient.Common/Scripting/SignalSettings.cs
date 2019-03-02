using System.Collections.Generic;
using System.ComponentModel;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;


namespace TradingClient.Common
{
    public class SignalSettings : ScriptingSettingsBase, ISignalSettings
    {
        [Browsable(false)]
        public override ScriptingType ScriptType => ScriptingType.Signal;
    }
}