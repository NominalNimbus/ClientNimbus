using System;
using System.ComponentModel;

namespace TradingClient.Data.Contracts
{
    public class SignalSelection : ICloneable, INotifyPropertyChanged
    {
        private bool _isSimulated;
        private string _symbol;
        private string _dataFeed;
        private TimeFrame _timeFrame;
        private int _interval;
        private int _barsCount;
        private byte _level;

        private int _marketDataSlot;
        private int _leverage;
        private decimal _slippage;

        public bool IsSimulated
        {
            get { return _isSimulated; }
            set
            {
                if (value.Equals(_isSimulated))
                    return;
                _isSimulated = value;
                OnPropertyChanged("IsSimulated");
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

        public TimeFrame TimeFrame
        {
            get { return _timeFrame; }
            set
            {
                if (value == _timeFrame)
                    return;
                _timeFrame = value;
                OnPropertyChanged("TimeFrame");
            }
        }

        public int Interval
        {
            get { return _interval; }
            set
            {
                if (value != _interval)
                {
                    _interval = value;
                    OnPropertyChanged("Interval");
                }
            }
        }

        public int BarCount
        {
            get { return _barsCount; }
            set
            {
                if (value == _barsCount)
                    return;
                _barsCount = value;
                OnPropertyChanged("BarsCount");
            }
        }

        public byte Level
        {
            get { return _level; }
            set
            {
                if (value != _level)
                {
                    _level = value;
                    OnPropertyChanged("Level");
                }
            }
        }

        public int MarketDataSlot
        {
            get { return _marketDataSlot; }
            set
            {
                if (value != _marketDataSlot)
                {
                    _marketDataSlot = value;
                    OnPropertyChanged("MarketDataSlot");
                }
            }
        }

        public int Leverage
        {
            get { return _leverage; }
            set
            {
                if (value != _leverage)
                {
                    _leverage = value;
                    OnPropertyChanged("Leverage");
                }
            }
        }

        public decimal Slippage
        {
            get { return _slippage; }
            set
            {
                if (value != _slippage)
                {
                    _slippage = value;
                    OnPropertyChanged("Slippage");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public SignalSelection()
        {
        }

        public SignalSelection(string dataFeed, string symbol)
        {
            IsSimulated = true;
            DataFeed = dataFeed;
            Symbol = symbol;
            BarCount = 100;
            TimeFrame = TimeFrame.Minute;
            Interval = 1;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}