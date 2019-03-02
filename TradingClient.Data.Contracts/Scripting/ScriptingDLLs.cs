using System;

namespace TradingClient.Data.Contracts
{
    public class ScriptingDLLs : EventArgs
    {
        public string CommonObjectsDllVersion { get; set; }

        public string ScriptingDllVersion { get; set; }

        public string BacktesterDllVersion { get; set; }

        public byte[] CommonObjectsDll { get; set; }

        public byte[] ScriptingDll { get; set; }

        public byte[] BacktesterDll { get; set; }
    }
}