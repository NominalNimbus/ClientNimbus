using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TradingClient.Interfaces;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public abstract class AlertBaseViewModel<T> : ViewModelBase, IAlertBaseViewModel<T>
    {
        #region Fields

        protected readonly string _scriptId;
        protected T _selectedItem;
        protected bool _isActivated;

        #endregion // Fields

        #region Properties

        protected IApplicationCore Core { get; }

        public T SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (value?.Equals(_selectedItem) == true)
                    return;

                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
            }
        }

        public bool Activated
        {
            get { return _isActivated; }
            set
            {
                if (_isActivated == value)
                    return;

                _isActivated = value;
                OnPropertyChanged(nameof(Activated));
            }
        }

        public abstract string Title { get; }

        public ObservableCollection<T> Items { get; private set; }

        #endregion // Properties

        #region Commands

        public ICommand CopyAllCommand { get; private set; }

        public ICommand ClearAllCommand { get; private set; }

        public ICommand CloseCommand { get; private set; }

        #endregion // Commands

        #region Constructors

        public AlertBaseViewModel(IApplicationCore c, string scriptingId)
        {
            Core = c;
            _scriptId = scriptingId;
            Items =GetCollections();
            if (Items.Count > 0)
                SelectedItem = Items.First();

            CopyAllCommand = new RelayCommand(() => CopyAll(), () => Items.Count > 0);
            ClearAllCommand = new RelayCommand(() => Core.ViewFactory.BeginInvoke(() => ClearAll()), () => Items.Count > 0);
            CloseCommand = new RelayCommand(() => DialogResult = false);
        }

        #endregion // Constructors

        #region Command Handlers

        private void CopyAll()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var a in Items)
                sb.Append(ConvertItemToString(a));

            try
            {
                System.Windows.Clipboard.SetText(sb.ToString());
            }
            catch { }
        }

        private void ClearAll()
        {
            ClearItems();
            Items.Clear();
            SelectedItem = default(T);
        }

        #endregion // Command Handlers

        #region IAlertBaseViewModel

        public void ShowNewItem(string message)
        {
            var newAlert  = CreateNewItem(message);
            Core.ViewFactory.BeginInvoke(() =>
            {
                Items.Insert(0, newAlert);
                SelectedItem = newAlert;
            });
        }

        #endregion // IAlertViewModel

        #region Virtual methods

        protected abstract ObservableCollection<T> GetCollections();

        protected abstract T CreateNewItem(string message);

        protected abstract string ConvertItemToString(T item);

        protected abstract void ClearItems();

        #endregion //Virtual methods
    }
}
