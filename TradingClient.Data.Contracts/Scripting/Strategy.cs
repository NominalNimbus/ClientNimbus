using System.Collections.ObjectModel;
using System.ComponentModel;

namespace TradingClient.Data.Contracts
{
    public class Strategy : INotifyPropertyChanged
    {
        private string _name;
        private StrategyBacktestSettings _btSettings;

        public int ID { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public Portfolio Parent { get; set; }

        public ObservableCollection<string> Datafeeds { get; set; }

        public ObservableCollection<Signal> Signals { get; set; }

        public decimal ExposedBalance { get; set; }

        public StrategyBacktestSettings BacktestSettings
        {
            get => _btSettings;
            set
            {
                if (value != _btSettings)
                {
                    _btSettings = value;
                    OnPropertyChanged("BacktestSettings");
                }
            }
        }

        public Strategy Clone()
        {
            return new Strategy
            {
                Name = Name,
                Parent = Parent,
                ExposedBalance = ExposedBalance,
                Datafeeds = Datafeeds != null ? new ObservableCollection<string>(Datafeeds) : null,
                Signals = Signals != null ? new ObservableCollection<Signal>(Signals) : null
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
