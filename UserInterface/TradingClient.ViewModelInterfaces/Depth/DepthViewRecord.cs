using System.ComponentModel;

namespace TradingClient.ViewModelInterfaces
{
    public class DepthViewRecord : INotifyPropertyChanged
    {
        private decimal _bid;
        private decimal _buyVolume;
        private decimal _sellVolume;
        private decimal _dailyVolume;
        private decimal _buyScale;
        private decimal _sellScale;
        private decimal _dailyScale;
        private decimal _ask;

        public decimal Bid
        {
            get { return _bid; }
            set
            {
                if (value == _bid)
                    return;
                _bid = value;
                OnPropertyChanged("Bid");
            }
        }

        public decimal Ask
        {
            get { return _ask; }
            set
            {
                if (value == _ask)
                    return;
                _ask = value;
                OnPropertyChanged("Ask");
            }
        }

        public decimal BuyVolume
        {
            get { return _buyVolume; }
            set
            {
                if (value == _buyVolume)
                    return;
                _buyVolume = value;
                OnPropertyChanged("BuyVolume");
            }
        }

        public decimal SellVolume
        {
            get { return _sellVolume; }
            set
            {
                if (value == _sellVolume)
                    return;
                _sellVolume = value;
                OnPropertyChanged("SellVolume");
            }
        }

        public decimal DailyVolume
        {
            get { return _dailyVolume; }
            set
            {
                if (value == _dailyVolume)
                    return;
                _dailyVolume = value;
                OnPropertyChanged("DailyVolume");
            }
        }

        public decimal BuyScale
        {
            get { return _buyScale; }
            set
            {
                if (value == _buyScale)
                    return;
                _buyScale = value;
                OnPropertyChanged("BuyScale");
            }
        }

        public decimal SellScale
        {
            get { return _sellScale; }
            set
            {
                if (value == _sellScale)
                    return;
                _sellScale = value;
                OnPropertyChanged("SellScale");
            }
        }

        public decimal DailyScale
        {
            get { return _dailyScale; }
            set
            {
                if (value == _dailyScale)
                    return;
                _dailyScale = value;
                OnPropertyChanged("DailyScale");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}