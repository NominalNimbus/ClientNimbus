using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingClient.Data.Contracts
{
    public class CreateSimulatedBrokerAccountInfo
    {
        public CreateSimulatedBrokerAccountInfo(string brokerName)
        {
            BrokerName = brokerName;
        }

        public string BrokerName { get; }

        public string AccountName { get; set; }

        public string Currency { get; set; }

        public int Ballance { get; set; }
    }
}
