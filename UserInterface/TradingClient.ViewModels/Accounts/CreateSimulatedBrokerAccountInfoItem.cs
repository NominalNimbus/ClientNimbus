using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingClient.Data.Contracts;

namespace TradingClient.ViewModels
{
    public class CreateSimulatedBrokerAccountInfoItem
    {
        public CreateSimulatedBrokerAccountInfoItem(string brokerName)
        {
            Account = new CreateSimulatedBrokerAccountInfo(brokerName);
        }

        [Browsable(false)]
        public CreateSimulatedBrokerAccountInfo Account { get; }

        [Browsable(false)]
        public ObservableCollection<string> Currencies { get; set; }

        [Display(Name = "Broker Name", Order = 5)]
        public string BrokerName => Account.BrokerName;

        [Display(Name = "Account Name", Order = 10)]
        public string AccountName
        {
            get => Account.AccountName;
            set => Account.AccountName = value;
        }

        [Display(Name = "Currency", Order = 20)]
        public string Currency
        {
            get => Account.Currency;
            set => Account.Currency = value;
        }

        [Display(Name = "Ballance", Order = 30)]
        public int Ballance
        {
            get => Account.Ballance;
            set => Account.Ballance = value;
        }
    }
}
