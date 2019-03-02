using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace TradingClient.ViewModelInterfaces
{
    public interface ICombinedPositionsViewModel : ITradesViewModel
    {
        IList<IPositionItem> Positions { get; }

        IPositionItem SelectedPosition { get; set; }

        ICommand ClosePositionCommand { get; }

    }
}