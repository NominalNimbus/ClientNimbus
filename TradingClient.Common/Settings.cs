using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using TradingClient.Interfaces;


namespace TradingClient.Common
{
    [ProtoContract]
    public sealed class Settings : ISettings
    {
        #region Properties

        [ProtoMember(1)]
        public string UserName { get; set; }

        [ProtoMember(2)]
        public byte[] WorkspaceData { get; set; }

        [ProtoMember(3)]
        public string HostAddress { get; set; }

        [ProtoMember(4)]
        public int Port { get; set; }

        [ProtoMember(5)]
        public List<AccountSetting> Accounts { get; set; }

        [ProtoIgnore]
        List<IAccountSetting> ISettings.Accounts
        {
            get => Accounts.ToList<IAccountSetting>();
            set => Accounts = value.Select(s => s as AccountSetting).ToList();
        }

        [ProtoMember(6)]
        public string DefaultBrokerName { get; set; }

        [ProtoMember(7)]
        public string DefaultBrokerAccount { get; set; }

        [ProtoMember(8)]
        public bool AutoLoginBrokerAccounts { get; set; }

        [ProtoIgnore]
        public bool? UseDefaultSignalBacktestSettings { get; set; }

        [ProtoIgnore]
        public bool? StartSignalWithoutConfiramtion { get; set; }
        
        #endregion // Properties

        #region Constructors

        public Settings()
        {
            UserName = string.Empty;
            HostAddress = "127.0.0.1";
            Port = 00001;
            Accounts = new List<AccountSetting>();
            DefaultBrokerName = string.Empty;
            DefaultBrokerAccount = string.Empty;
        }

        #endregion // Constructors
    }
}