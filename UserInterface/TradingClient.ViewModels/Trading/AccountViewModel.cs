using System.ComponentModel;
using TradingClient.Data.Contracts;

namespace TradingClient.ViewModels
{
    public class Account : INotifyPropertyChanged
    {
        private bool _isSelected;
        private AccountInfo _accountInfo;
        private bool _isEnabled;
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value.Equals(_isSelected))
                    return;
                _isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }

        public AccountInfo AccountInfo
        {
            get => _accountInfo;
            set
            {
                if (Equals(value, _accountInfo))
                    return;
                _accountInfo = value;
                OnPropertyChanged("AccountInfo");
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (value.Equals(_isEnabled))
                    return;
                _isEnabled = value;
                OnPropertyChanged("IsEnabled");
            }
        }
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
