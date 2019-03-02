using System;
using System.Collections.Generic;
using System.Linq;

namespace TradingClient.Data.Contracts
{
    public class Indicator : ICloneable
    {
        public string ID { get; set; }
            
        public string Name { get; set; }
            
        public string DisplayName { get; set; }
            
        public bool IsOverlay { get; set; }
            
        public List<Series> Series { get; set; }
            
        public List<ScriptingParameterBase> Parameters { get; set; }

        public object Clone()
        {
            var obj = MemberwiseClone() as Indicator;
            obj.Parameters = Parameters.Select(p => p.Clone() as ScriptingParameterBase).ToList();
            obj.Series = Series.ToList();
            return obj;
        }

        public override string ToString()
        {
            return String.Format("{0}({1})", DisplayName, ID);
        }

        public bool ParametersEquals(List<ScriptingParameterBase> @params)
        {
            return !(@params == null || @params.Any(parameter => Parameters.FirstOrDefault(p => p.Equals(parameter)) == null));
        }
    }
}
