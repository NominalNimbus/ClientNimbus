using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TradingClient.Common;

namespace TradingClient.ViewModelInterfaces
{
    public interface IScriptingLogViewModel : IDocumentViewModel
    {
        ObservableCollection<ScriptingLogItem> LogItems { get; }
        ICommand ClearCommand { get; }
    }
}