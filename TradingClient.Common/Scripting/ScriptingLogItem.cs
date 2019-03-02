using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingClient.Common
{
    public class ScriptingLogItem
    {
        public ScriptingLogItem(DateTime date, string message)
        {
            Date = date;
            Message = message;
        }

        public DateTime Date { get; }

        public string Message { get; }
    }
}
