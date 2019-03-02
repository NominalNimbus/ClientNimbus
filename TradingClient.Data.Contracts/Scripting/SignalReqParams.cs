using System.Collections.Generic;

namespace TradingClient.Data.Contracts
{
    public class SignalReqParams
    {
        public string FullName { get; set; }  //portfolio\strategy\signal
        public bool IsSimulated { get; set; }
        public List<ScriptingParameterBase> Parameters { get; set; }
        public List<SignalSelection> Selections { get; set; }
        public StrategyParams StrategyParameters { get; set; }
        public SignalBacktestSettings BacktestSettings { get; set; }
        public StrategyBacktestSettings StrategyBacktestSettings { get; set; }
        public List<PortfolioAccount> Accounts { get; set; }
    }
}
