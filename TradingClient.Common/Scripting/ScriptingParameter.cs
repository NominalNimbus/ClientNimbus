using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using TradingClient.Interfaces;

namespace TradingClient.Common
{
    public class ScriptingParameter : IScriptingParameter
    {
        #region Constructors

        public ScriptingParameter(ScriptingParameterTypes type, string name = "", object value = null, string description = "")
        {
            Type = type;
            Name = name;
            Value = value;
            Description = description;
        }

        #endregion //Constructors

        #region Properties

        [Browsable(false)]
        public ScriptingParameterTypes Type { get; set; }

        [Display(Name = "Name", Order = 10)]
        public string Name { get; set; }

        [Display(Name = "Value", Order = 20)]
        public object Value { get; set; }

        [Display(Name = "Description", Order = 30)]
        public string Description { get; set; }
        
        #endregion //Properties
    }
}