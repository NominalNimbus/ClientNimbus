using System;

namespace TradingClient.Data.Contracts
{
    public class CurrencyBasedCoefficient : ICloneable
    {
        public decimal EUR { get; set; }
        public decimal USD { get; set; }
        public decimal GBP { get; set; }

        public CurrencyBasedCoefficient()
        {
            EUR = 1;
            USD = 1;
            GBP = 1;
        }

        public object Clone() => 
            MemberwiseClone();

        public override bool Equals(object obj)
        {
            var ret = false;
            var value = obj as CurrencyBasedCoefficient;
            if (value != null)
                ret = (EUR == value.EUR && USD == value.USD && GBP == value.GBP);
            return ret;
        }

        public override int GetHashCode() =>
            EUR.GetHashCode() ^ USD.GetHashCode() ^ GBP.GetHashCode();
    }
}