using System;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using TradingClient.Common;

namespace TradingClient.ViewModels
{
    public class EditStringViewModel : ViewModelBase
    {
        private string _title;
        private string _value;
        private bool? _dialogResult;
        private ResizeMode _resizeMode;
        private Func<string, bool> _stringValueValidator;

        public string Title
        {
            get => _title;
            set
            {
                if (value != _title)
                {
                    _title = value;
                    OnPropertyChanged("Title");
                }
            }
        }

        public ResizeMode ResizeMode
        {
            get => _resizeMode;
            set
            {
                if (value != _resizeMode)
                {
                    _resizeMode = value;
                    OnPropertyChanged("ResizeMode");
                }
            }
        }

        public string Value
        {
            get => _value?.Trim();
            set
            {
                if (value != _value)
                {
                    _value = value;
                    OnPropertyChanged("Value");
                }
            }
        }

        public bool? DialogResult
        {
            get => _dialogResult;
            set
            {
                if (value != _dialogResult)
                {
                    _dialogResult = value;
                    OnPropertyChanged("DialogResult");
                }
            }
        }

        public ICommand OkCommand { get; private set; }

        public ICommand CancelCommand { get; private set; }

        public EditStringViewModel(string value, string title, ResizeMode resizeMode, Func<string, bool> validator = null)
        {
            ResizeMode = resizeMode;
            Value = value ?? string.Empty;
            Title = title ?? string.Empty;
            _stringValueValidator = validator;
            OkCommand = new RelayCommand(OkExecute, () => !string.IsNullOrWhiteSpace(_value));
            CancelCommand = new RelayCommand(() => { DialogResult = false; });
        }

        private void OkExecute()
        {
            if (_stringValueValidator?.Invoke(Value) == false)
                return;

            DialogResult = true;
        }
    }
}
