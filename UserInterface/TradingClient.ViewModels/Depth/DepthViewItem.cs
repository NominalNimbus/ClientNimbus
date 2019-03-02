using System.Collections.ObjectModel;
using System.ComponentModel;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public class DepthViewItem : IDepthViewItem, INotifyPropertyChanged
    {
        private string _broker;
        private string _symbol;
        private string _dataFeed;

        public ObservableCollection<DepthViewRecord> Records { get; set; }

        public string Broker
        {
            get { return _broker; }
            set
            {
                if (value == _broker)
                    return;
                _broker = value;
                OnPropertyChanged("Broker");
            }
        }

        public string DataFeed
        {
            get { return _dataFeed; }
            set
            {
                if (value == _dataFeed)
                    return;
                _dataFeed = value;
                OnPropertyChanged("DataFeed");
            }
        }

        public string Symbol
        {
            get { return _symbol; }
            set
            {
                if (value == _symbol)
                    return;
                _symbol = value;
                OnPropertyChanged("Symbol");
            }
        }

        public DepthViewItem()
        {
            Records = new ObservableCollection<DepthViewRecord>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
