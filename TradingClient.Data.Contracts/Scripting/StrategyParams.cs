
namespace TradingClient.Data.Contracts
{
    public class StrategyParams
    {
        public int StrategyID { get; }
        public decimal ExposedBalance { get; }
        
        public StrategyParams(Strategy strategy)
        {
            StrategyID = strategy.ID;
            ExposedBalance = strategy.ExposedBalance;
        }
    }
}