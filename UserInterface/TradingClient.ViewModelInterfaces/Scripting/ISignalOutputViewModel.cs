using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingClient.Data.Contracts;

namespace TradingClient.ViewModelInterfaces
{
    public interface ISignalOutputViewModel : IAlertBaseViewModel<ScriptingLogData>
    {
    }
}
