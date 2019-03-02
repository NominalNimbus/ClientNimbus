using System;
using ProtoBuf;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;

namespace TradingClient.Common
{
    [ProtoContract]
    public class AccountSetting : IAccountSetting
    {
        #region Properties

        [ProtoMember(1)]
        public string UserName { get; set; }
        [ProtoMember(2)]
        public string Account{ get; set; }
        [ProtoMember(3)]
        public string BrokerName { get; set; }
        [ProtoMember(4)]
        public string DataFeedName { get; set; }
        [ProtoMember(5)]
        public string Url { get; set; }
        [ProtoMember(6)]
        public string Key { get; set; }

        #endregion //Properties

        #region Constructors

        public AccountSetting()
            => Reset();

        public AccountSetting(AccountInfo account)
        {
            if (account == null)
                Reset();
            else
                SetFromAccountInfo(account);
        }

        #endregion //Constructors

        public AccountInfo ToAccountInfo()
        {
            return new AccountInfo
            {
                UserName = this.UserName,
                Account = Account,
                Password = Cryptography.Decrypt(this.Key),
                BrokerName = this.BrokerName,
                DataFeedName = this.DataFeedName,
                Url = this.Url
            };
        }

        #region Helper Methods

        private void Reset()
        {
            UserName = String.Empty;
            Account = String.Empty;
            BrokerName = String.Empty;
            DataFeedName = String.Empty;
            Url = String.Empty;
            Key = String.Empty;
        }

        private void SetFromAccountInfo(AccountInfo account)
        {
            Account = account.Account;
            UserName = account.UserName;
            BrokerName = account.BrokerName;
            DataFeedName = account.DataFeedName;
            Url = account.Url;
            Key = Cryptography.Encrypt(account.Password);
        }

        #endregion //Helper Methods

    }
}