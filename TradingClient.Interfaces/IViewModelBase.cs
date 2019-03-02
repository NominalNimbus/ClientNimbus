using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingClient.Interfaces
{
    public interface IViewModelBase : IDisposable
    {
        bool? DialogResult { get; set; }
    }
}
