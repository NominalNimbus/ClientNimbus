using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingClient.Data.Contracts;

namespace TradingClient.Common
{
    public class SimulatedAccountBrokerInfoItem : AccountBrokerInfo
{
        public SimulatedAccountBrokerInfoItem(AccountInfo account, ObservableCollection<string> accountList)
            : base(account, "Demo")
        {
            AccountList = accountList;
        }

        #region Properties

        public override string Account => AccountName;

        [Display(Name = "DataFeed", Order = 20)]
        public ObservableCollection<string> DataFeeds { get; set; }

        [Browsable(false)]
        public ObservableCollection<string> AccountList { get; set; }

        [Display(Name = "Account", Order = 30)]
        public string AccountName
        {
            get => AccountInfo.Account;
            set
            {
                if (AccountInfo.Account == value)
                    return;

                AccountInfo.Account = value;
                OnPropertyChanged(nameof(AccountName));
                OnPropertyChanged(nameof(Account));
            }
        }

        [Display(Description = " ", Order = 40)]
        public object AddAccount { get; set; }

        #endregion Properties

        #region Methods

        public override string Validate()
        {
            if (string.IsNullOrEmpty(AccountInfo.DataFeedName))
            {
                return "Please set datafeed";
            }
            if (string.IsNullOrEmpty(AccountInfo.Account))
            {
                return "Please set account";
            }
            return string.Empty;
        }

        #endregion //Methods
    }
}
