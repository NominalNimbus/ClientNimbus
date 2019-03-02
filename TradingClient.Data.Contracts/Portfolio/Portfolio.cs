using System.Collections.ObjectModel;
using System.ComponentModel;

namespace TradingClient.Data.Contracts
{
    public class Portfolio : INotifyPropertyChanged
    {
        private string _name;

        private string _baseCurrency;

        public int ID { get; set; }

        public string User { get; set; }

        public ObservableCollection<PortfolioAccount> Accounts { get; set; }

        public ObservableCollection<Strategy> Strategies { get; set; }

        public string Name
        {
            get { return _name; }
            set
            {
                if (value == _name)
                    return;
                _name = value;
                OnPropertyChanged("Name");
            }
        }

        public string BaseCurrency
        {
            get { return _baseCurrency; }
            set
            {
                if (value == _baseCurrency)
                    return;
                _baseCurrency = value;
                OnPropertyChanged("BaseCurrency");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}