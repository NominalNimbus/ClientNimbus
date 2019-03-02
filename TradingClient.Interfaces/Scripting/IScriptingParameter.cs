using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingClient.Interfaces
{
    public interface IScriptingParameter
    {
        ScriptingParameterTypes Type { get; set; }

        string Name { get; set; }

        object Value { get; set; }

        string Description { get; set; }

    }
}
