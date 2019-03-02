using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TradingClient.Data.Contracts
{
    public class AccountInfo : INotifyPropertyChanged, ICloneable
    {
        #region Members

        private bool _isDefault;
        private string _brokerName;
        private string _dataFeedName;
        private string _account;
        private string _userName;
        private string _password;
        private string _url;
        private decimal _balance;
        private decimal _margin;
        private decimal _equity;
        private decimal _profit;
        private CurrencyBasedCoefficient _coefficient;
        private int _balanceIncrement;

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion //Members

        #region Constructor

        public AccountInfo()
        {
            ID = string.Empty;
            BrokerName = string.Empty;
            DataFeedName = string.Empty;
            Account = string.Empty;
            Url = string.Empty;
            UserName = string.Empty;
            Password = string.Empty;
            IsDefault = false;
            Currency = string.Empty;
            IsMarginAccount = true;
            AvailableSymbols = new List<Security>();
            Coefficient = new CurrencyBasedCoefficient();
            BalanceDecimals = 2;
        }

        #endregion //Constructor

        #region Properties

        public string ID { get; set; }

        public string Currency { get; set; }

        public bool IsMarginAccount { get; set; }

        public List<Security> AvailableSymbols { get; private set; }

        public bool IsDefault
        {
            get => _isDefault;
            set
            {
                if (value == _isDefault)
                    return;

                _isDefault = value;
                OnPropertyChanged(nameof(IsDefault));
            }
        }

        public string BrokerName
        {
            get => _brokerName;
            set
            {
                if (value == _brokerName)
                    return;
                _brokerName = value;
                OnPropertyChanged(nameof(BrokerName));
            }
        }

        public string DataFeedName
        {
            get => _dataFeedName;
            set
            {
                if (value == _dataFeedName)
                    return;
                _dataFeedName = value;
                OnPropertyChanged(nameof(DataFeedName));
            }
        }
        
        public string Account
        {
            get => _account;
            set
            {
                if (value == _account)
                    return;
                _account = value;
                OnPropertyChanged(nameof(Account));
            }
        }

        public string UserName
        {
            get => _userName;
            set
            {
                if (value == _userName)
                    return;
                _userName = value;
                OnPropertyChanged(nameof(UserName));
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (value == _password)
                    return;
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        public string Url
        {
            get => _url;
            set
            {
                if (value == _url)
                    return;
                _url = value;
                OnPropertyChanged(nameof(Url));
            }
        }

        public decimal Balance
        {
            get => _balance;
            set
            {
                if (value == _balance)
                    return;
                _balance = value;
                OnPropertyChanged(nameof(Balance));
            }
        }

        public decimal Margin
        {
            get => _margin;
            set
            {
                if (value == _margin)
                    return;
                _margin = value;
                OnPropertyChanged(nameof(Margin));
            }
        }

        public decimal Equity
        {
            get => _equity;
            set
            {
                if (value == _equity)
                    return;
                _equity = value;
                OnPropertyChanged(nameof(Equity));
            }
        }

        public decimal Profit
        {
            get => _profit;
            set
            {
                if (value == _profit)
                    return;
                _profit = value;
                OnPropertyChanged(nameof(Profit));
            }
        }

        public CurrencyBasedCoefficient Coefficient
        {
            get => _coefficient;
            set
            {
                if (Equals(value, _coefficient))
                    return;
                _coefficient = value;
                OnPropertyChanged(nameof(Coefficient));
            }
        }

        public int BalanceDecimals
        {
            get => _balanceIncrement;
            set
            {
                if (value == _balanceIncrement)
                    return;
                _balanceIncrement = value;
                OnPropertyChanged(nameof(BalanceDecimals));
            }
        }

        #endregion //Properties

        #region Methods

        public override int GetHashCode() =>
            ID.GetHashCode() ^ BrokerName.GetHashCode() ^ DataFeedName.GetHashCode() ^ UserName.GetHashCode() ^ Password.GetHashCode() ^ Account.GetHashCode();

        public override bool Equals(object obj)
        {
            if (!(obj is AccountInfo info))
                return false;

            return Equals(info.UserName, UserName) && Equals(info.Password, Password) && Equals(info.Account, Account) && 
                Equals(info.BrokerName, BrokerName) && Equals(info.DataFeedName, DataFeedName);
        }

        public override string ToString() =>
            $"{BrokerName} - {UserName} ({ID})";
                
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public object Clone()
        {
            return MemberwiseClone();
        }

        #endregion //Methods
    }
}