using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TradingClient.ViewModelInterfaces
{
    public interface IAlertBaseViewModel<T>
    {
        ICommand CopyAllCommand { get; }
        ICommand ClearAllCommand { get; }
        bool Activated { get; set; }
        string Title { get; }
        ObservableCollection<T> Items { get; }

        void ShowNewItem(string message);
    }
}
