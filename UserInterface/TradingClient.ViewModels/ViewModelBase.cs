using System;
using System.ComponentModel;
using TradingClient.Common;
using TradingClient.Interfaces;

namespace TradingClient.ViewModels
{
    public abstract class ViewModelBase : Observable, IViewModelBase
    {
        #region Fields

        private bool? _dialogResult;

        #endregion // Fields

        #region Properites

        public bool? DialogResult
        {
            get { return _dialogResult; }
            set
            {
                _dialogResult = value;
                OnPropertyChanged(nameof(DialogResult));
            }
        }

        #endregion // Properites

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        #endregion // IDisposable
    }
}