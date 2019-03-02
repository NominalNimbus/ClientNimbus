using System.Collections.Generic;
using ProtoBuf;

namespace TradingClient.Interfaces
{
    public interface ISettings
    {
        string UserName { get; set; }

        string HostAddress { get; set; }
        
        int Port { get; set; }

        byte[] WorkspaceData { get; set; }

        List<IAccountSetting> Accounts { get; set; }

        string DefaultBrokerName { get; set; }

        string DefaultBrokerAccount { get; set; }

        bool AutoLoginBrokerAccounts { get; set; }

        bool? UseDefaultSignalBacktestSettings { get; set; }

        bool? StartSignalWithoutConfiramtion { get; set; }
    }
}