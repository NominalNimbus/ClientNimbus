using System;
using System.Collections.Generic;

namespace TradingClient.Data.Contracts
{
    public class Series
    {
        public Series()
        {
            Values = new Dictionary<DateTime, double>();
            ID = Guid.NewGuid().ToString();
            Name = string.Empty;
        }

        public Series(string name, string id)
            : this()
        {
            Name = name;
            ID = id;
        }

        public string ID { get; private set; }

        public string Name { get; set; }

        public Dictionary<DateTime, double> Values { get; private set; }
    }
}