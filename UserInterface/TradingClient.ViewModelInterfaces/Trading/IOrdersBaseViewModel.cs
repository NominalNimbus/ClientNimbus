using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TradingClient.ViewModelInterfaces
{
    public interface IOrdersBaseViewModel : ITradesViewModel
    {
        ObservableCollection<IOrderItem> Orders { get; }

        IOrderItem SelectedOrder { get; set; }

        ICommand ModifyCommand { get; }
    }
}
