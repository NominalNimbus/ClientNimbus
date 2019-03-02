using System;
using System.Collections.Generic;

namespace TradingClient.Data.Contracts
{
    public class SeriesForUpdate
    {
        public string SeriesID { get; set; }

        public string InsdicatorName { get; set; }

        public Dictionary<DateTime, double> Values { get; private set; }

        public SeriesForUpdate()
        {
            InsdicatorName = String.Empty;
            SeriesID = String.Empty;
            Values = new Dictionary<DateTime, double>();
        }

        public SeriesForUpdate(string id, string indicatorName, Dictionary<DateTime, double> values)
        {
            InsdicatorName = indicatorName;
            SeriesID = id;
            Values = new Dictionary<DateTime, double>(values != null ? values.Count : 0);
            if (values != null && values.Count != 0)
            {
                foreach (var item in values)
                    Values.Add(item.Key, item.Value);
            }
        }
    }
}