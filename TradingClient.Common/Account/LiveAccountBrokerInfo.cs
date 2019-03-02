using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingClient.Data.Contracts;

namespace TradingClient.Common
{
    public class LiveAccountBrokerInfoItem : AccountBrokerInfo
    {
        public LiveAccountBrokerInfoItem(AccountInfo account, string url, string group = "Live")
            : base(account, group)
        {
            Url = url;
        }

        #region Properties

        public override string Account => UserName;

        [Display(Name = "User Name", Order = 20)]
        public string UserName
        {
            get => AccountInfo.UserName;
            set
            {
                if (AccountInfo.UserName == value)
                    return;

                AccountInfo.UserName = value;
                OnPropertyChanged(nameof(UserName));
                OnPropertyChanged(nameof(Account));
            }
        }

        [Display(Name = "Password", Order = 30)]
        public string Password
        {
            get => AccountInfo.Password;
            set
            {
                if (AccountInfo.Password == value)
                    return;

                AccountInfo.Password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        [Display(Name = "Url", Order = 40)]
        public string Url { get; }

        #endregion //Properties
        
        #region Methods

        public override string Validate()
        {
            if (string.IsNullOrEmpty(AccountInfo.UserName))
            {
                return "Please set username";
            }
            if (string.IsNullOrEmpty(AccountInfo.Password))
            {
                return "Please set password";
            }
            return string.Empty;
        }

        #endregion //Methods
    }
}
