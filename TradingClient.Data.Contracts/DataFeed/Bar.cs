using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace TradingClient.Data.Contracts
{
    [Serializable]
    [DebuggerDisplay("${Close} @ {Timestamp.ToString(\"MMMd HH:mm:ss\"), nq}")]
    public class Bar
    {

        #region Properties

        public DateTime Timestamp { get; set; }

        public decimal OpenBid { get; set; }

        public decimal OpenAsk { get; set; }

        public decimal Open => OpenBid == 0M ? OpenAsk : (OpenAsk == 0M ? OpenBid : ((OpenBid + OpenAsk) / 2M));

        public decimal HighBid { get; set; }

        public decimal HighAsk { get; set; }

        public decimal High => HighBid == 0M ? HighAsk : (HighAsk == 0M ? HighBid : ((HighBid + HighAsk) / 2M));

        public decimal LowBid { get; set; }

        public decimal LowAsk { get; set; }

        public decimal Low => LowBid == 0M ? LowAsk : (LowAsk == 0M ? LowBid : ((LowBid + LowAsk) / 2M));

        public decimal CloseBid { get; set; }

        public decimal CloseAsk { get; set; }

        public decimal Close => CloseBid == 0M ? CloseAsk : (CloseAsk == 0M ? CloseBid : ((CloseBid + CloseAsk) / 2M));

        public long VolumeBid { get; set; }

        public long VolumeAsk { get; set; }

        public long Volume => VolumeBid == 0L ? VolumeAsk : (VolumeAsk == 0L ? VolumeBid : (long)((VolumeBid + VolumeAsk) / 2.0));

        #endregion //Properties

        public Bar()
        {
        }

        public Bar(DateTime date, decimal bid, decimal ask, long bidSize, long askSize)
        {
            this.Timestamp = date;
            this.OpenBid = bid;
            this.OpenAsk = ask;
            this.HighBid = bid;
            this.HighAsk = ask;
            this.LowBid = bid;
            this.LowAsk = ask;
            this.CloseBid = bid;
            this.CloseAsk = ask;
            this.VolumeBid = bidSize;
            this.VolumeAsk = askSize;
        }
    }
}