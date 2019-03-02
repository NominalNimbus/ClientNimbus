namespace TradingClient.Data.Contracts
{
    public enum SignalAction
    {
        StopExecution = 0,
        SetSimulatedOn = 1,
        SetSimulatedOff = -1,
        PauseBacktest = 2,
        ResumeBacktest = -2
    }
}
