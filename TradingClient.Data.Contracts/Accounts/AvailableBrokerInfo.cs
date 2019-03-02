using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingClient.Data.Contracts
{
    public class AvailableBrokerInfo
    {
        public string BrokerName { get; set; }
        public string DataFeedName { get; set; }
        public List<string> Accounts { get; set; }
        public string Url { get; set; }
        public BrokerType BrokerType { get; set; }

        public AvailableBrokerInfo()
        {

        }

        public AvailableBrokerInfo(string broker, string dataFeed, List<string> accounts, string url, BrokerType brokerType)
        {
            BrokerName = broker;
            DataFeedName = dataFeed;
            Accounts = accounts;
            Url = url;
            BrokerType = brokerType;
        }

        public override string ToString() => BrokerName;
    }
}
