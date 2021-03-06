﻿using System.Collections.ObjectModel;
using System.Windows.Input;

namespace TradingClient.ViewModelInterfaces
{
    public interface IIndividualPositionsViewModel : IOrdersBaseViewModel
    {
        ICommand PlaceOpposite { get; }
    }
}