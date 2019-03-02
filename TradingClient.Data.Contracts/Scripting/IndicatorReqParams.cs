
namespace TradingClient.Data.Contracts
{
    public class IndicatorReqParams
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string DataFeed { get; set; }
        public TimeFrame Timeframe { get; set; }
        public int Interval { get; set; }
        public int BarCount { get; set; }
        public System.DateTime From { get; set; }
        public System.DateTime To { get; set; }
        public byte Level { get; set; }
        public bool? IncludeWeekendData { get; set; }
        public PriceType PriceType { get; set; }
        public System.Collections.Generic.List<ScriptingParameterBase> Parameters { get; set; }
    }
}
