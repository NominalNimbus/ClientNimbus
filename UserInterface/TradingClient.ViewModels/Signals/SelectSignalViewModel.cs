using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;

namespace TradingClient.ViewModels
{
    public class SelectSignalViewModel : ViewModelBase
    {

        #region Properties

        public List<string> SelectedSignals { get; set; }

        public ObservableCollection<string> Signals { get; }

        #endregion

        #region Commands

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        public SelectSignalViewModel(IEnumerable<string> signalCollection)
        {
            Signals = new ObservableCollection<string>(signalCollection);

            OkCommand = new RelayCommand<object>(signals =>
            {
                SelectedSignals = ((IList)signals).Cast<string>().ToList();
                DialogResult = true;
            });

            CancelCommand = new RelayCommand(() => { DialogResult = false; });
        }
    }
}
