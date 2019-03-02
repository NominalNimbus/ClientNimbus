using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TradingClient.Data.Contracts
{
    public class CsvSerializer<T>
    {
        private const string Separator = ",";

        public void Serialize(string file, IEnumerable<T> items)
        {
            var _workItem = new CsvSerializeSubData(null, typeof(T));
            var text = Join(_workItem.GetHeaders(items));
            foreach (var item in items)
            {
                text += Environment.NewLine + Join(_workItem.GetValue(item));
            }

            File.WriteAllText(file, text);
        }

        private string Join(IEnumerable<string> data) => string.Join(Separator, data);
        
    }
}
