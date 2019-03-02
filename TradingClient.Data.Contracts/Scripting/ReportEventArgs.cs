using System;
using System.Collections.Generic;

namespace TradingClient.Data.Contracts
{
    public class ReportEventArgs : EventArgs
    {
        public string Id { get; set; }
        public List<ReportField> ReportFields { get; set; }
    }

    public class ReportField
    {
        public string SignalName { get; set; }
        public string Symbol { get; set; }
        public Side Side { get; set; }
        public OrderType OrderType { get; set; }
        public decimal Quantity { get; set; }
        public TimeInForce TimeInForce { get; set; }
        public Status Status { get; set; }
        public DateTime SignalGeneratedDateTime { get; set; }
        public DateTime OrderGeneratedDate { get; set; }
        public DateTime OrderFilledDate { get; set; }
        public DateTime DBOrderEntryDate { get; set; }
        public DateTime DBSignalEntryDate { get; set; }
        public int SignalToOrderSpan { get; set; }
        public int OrderFillingDelay { get; set; }
    }
}
