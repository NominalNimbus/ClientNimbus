using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingClient.Data.Contracts;

namespace TradingClient.Interfaces
{
    [ProtoContract]
    public interface IAccountSetting
    {
        string UserName { get; set; }
        
        string Account { get; set; }
        
        string BrokerName { get; set; }
        
        string DataFeedName { get; set; }
        
        string Url { get; set; }
        
        string Key { get; set; }

        AccountInfo ToAccountInfo();
    }
}
