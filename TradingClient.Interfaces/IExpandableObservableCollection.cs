using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingClient.Interfaces
{
    public interface IExpandableObservableCollection<T> : IList<T>
    {
        void AddRange(IEnumerable<T> items);
    }
}
