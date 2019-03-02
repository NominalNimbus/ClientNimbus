using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;

namespace TradingClient.Common
{
    public abstract class AccountBrokerInfo : Observable
    {
        protected AccountBrokerInfo(AccountInfo account, string group)
        {
            AccountInfo = account;
            Group = group;
        }

        #region Properties

        [Browsable(false)]
        public AccountInfo AccountInfo { get; set; }

        [Display(Name = "Broker", Order = 10)]
        public string BrokerName => AccountInfo.BrokerName;

        [Browsable(false)]
        public string DataFeed
        {
            get => AccountInfo.DataFeedName;
            set
            {
                if (AccountInfo.DataFeedName == value)
                    return;

                AccountInfo.DataFeedName = value;
                OnPropertyChanged(nameof(DataFeed));
            }
        }

        [Browsable(false)]
        public virtual string Account { get; }

        [Browsable(false)]
        public string Group { get; set; }

        #endregion //Properties

        #region Methods

        public abstract string Validate();

        public bool IsDuplicateAccount(string broker, string account) =>
            BrokerName == broker && Account == account;
        
        #endregion //Methods
    }
}
