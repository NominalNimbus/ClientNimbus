namespace TradingClient.Data.Contracts
{
    public enum State
    {
        New,            //created but not uploaded to server yet
        Stopped,        //uploaded to server but not executing
        Paused,         //running with IsSimulated=true
        Working,        //running (and IsSimulated=false)
        Backtesting,    //backtesting
        BacktestPaused  //backtesting (paused)
    }
}