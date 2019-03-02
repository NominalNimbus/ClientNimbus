
using System;

namespace TradingClient.Data.Contracts
{
    public class PortfolioAccount : ICloneable
    {
        public int ID { get; set; }

        public string Name { get; set; }
        
        public string BrokerName { get; set; }

        public string DataFeedName { get; set; }

        public string Account { get; set; }

        public string UserName { get; set; }

        public string DisplatyUserName => UserName + (string.IsNullOrEmpty(Account) ? string.Empty : $"({Account})");

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
