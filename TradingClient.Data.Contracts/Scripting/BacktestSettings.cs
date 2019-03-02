using System;
using System.ComponentModel;

namespace TradingClient.Data.Contracts
{
    public class SignalBacktestSettings : INotifyPropertyChanged
    {
        private DateTime _startDate;
        private DateTime _endDate;
        private int _barsBack;
        private string _barDataDirectory;
        private bool _useInstrBarCount;
        private bool _useBarCount;
        private bool _useTimeInterval;
        private bool _useBarData;

        public DateTime StartDate
        {
            get { return _startDate; }
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    OnPropertyChanged("StartDate");
                }
            }
        }

        public DateTime EndDate
        {
            get { return _endDate; }
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    OnPropertyChanged("EndDate");
                }
            }
        }

        public int BarsBack
        {
            get { return _barsBack; }
            set
            {
                if (_barsBack != value)
                {
                    _barsBack = value;
                    OnPropertyChanged("BarsBack");
                }
            }
        }

        public string BarDataDirectory
        {
            get { return _barDataDirectory; }
            set
            {
                if (_barDataDirectory != value)
                {
                    _barDataDirectory = value;
                    OnPropertyChanged("BarDataDirectory");
                }
            }
        }

        public bool UseInstrBarCount
        {
            get { return _useInstrBarCount; }
            set
            {
                if (_useInstrBarCount != value)
                {
                    _useInstrBarCount = value;
                    if (value)
                        UseBarCount = UseTimeInterval = false;
                    else if (!_useBarCount && !_useTimeInterval)
                        _useInstrBarCount = true;
                    OnPropertyChanged("UseInstrBarCount");
                }
            }
        }

        public bool UseBarCount
        {
            get { return _useBarCount; }
            set
            {
                if (_useBarCount != value)
                {
                    _useBarCount = value;
                    OnPropertyChanged("UseBarCount");
                    if (value)
                        UseInstrBarCount = UseTimeInterval = false;
                    else if (!_useInstrBarCount && !_useTimeInterval)
                        UseInstrBarCount = true;
                }
            }
        }

        public bool UseTimeInterval
        {
            get { return _useTimeInterval; }
            set
            {
                if (_useTimeInterval != value)
                {
                    _useTimeInterval = value;
                    OnPropertyChanged("UseTimeInterval");
                    if (value)
                        UseInstrBarCount = UseBarCount = false;
                    else if (!_useInstrBarCount && !_useBarCount)
                        UseInstrBarCount = true;
                }
            }
        }

        public bool UseBarData
        {
            get { return _useBarData; }
            set
            {
                if (_useBarData != value)
                {
                    _useBarData = value;
                    OnPropertyChanged("UseBarData");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public SignalBacktestSettings()
        {
            _useInstrBarCount = true;
        }
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class StrategyBacktestSettings : INotifyPropertyChanged
    {
        private decimal _initialBalance;
        private decimal _risk;
        private decimal _transactionCosts;

        public decimal InitialBalance
        {
            get { return _initialBalance; }
            set
            {
                if (_initialBalance != value)
                {
                    _initialBalance = value;
                    OnPropertyChanged("InitialBalance");
                }
            }
        }

        public decimal Risk
        {
            get { return _risk; }
            set
            {
                if (_risk != value)
                {
                    _risk = value;
                    OnPropertyChanged("Risk");
                }
            }
        }

        public decimal TransactionCosts
        {
            get { return _transactionCosts; }
            set
            {
                if (_transactionCosts != value)
                {
                    _transactionCosts = value;
                    OnPropertyChanged("TransactionCosts");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}