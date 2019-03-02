using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingClient.Interfaces
{
    public interface IUIManager
    {
        IViewFactory ViewFactory { get; }

        void RegisterViews();
    }
}
