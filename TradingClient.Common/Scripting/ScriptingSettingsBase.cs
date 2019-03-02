using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace TradingClient.Common
{
    public abstract class ScriptingSettingsBase : Observable, IScriptingSettings
    {
        private IExpandableObservableCollection<IScriptingParameter> _parameters;

        #region Properties

        [Display(Name = "Name", Order = 10)]
        public string Name { get; set; }

        [Display(Name = "Description", Order = 20)]
        public string Description { get; set; }

        [Display(Name = "Author", Order = 30)]
        public string Author { get; set; }

        [Display(Name = "Link", Order = 40)]
        public string Link { get; set; }

        [Browsable(false)]
        public abstract ScriptingType ScriptType { get; }

        [ExpandableObject]
        [Display(Name = "Parameters", Order = 50)]
        [RefreshProperties(RefreshProperties.All)]
        public IExpandableObservableCollection<IScriptingParameter> Parameters
        {
            get => _parameters;
            set => SetPropertyValue(ref _parameters, value, nameof(Parameters));
        }

        #endregion //Properties

        public ScriptingSettingsBase()
        {
            Name = string.Empty;
            Description = string.Empty;
            Author = string.Empty;
            Link = string.Empty;
            Parameters = new ExpandableObservableCollection<IScriptingParameter>();
        }

        public void RefreshParameters() =>
            Parameters = new ExpandableObservableCollection<IScriptingParameter>(Parameters);

        public virtual string ValidateSettings(IEnumerable<string> existingItemsName, IEnumerable<string> existingSolutionItemsName)
        {
            if (string.IsNullOrWhiteSpace(Name))
                return "Name is required parameter";

            if (existingItemsName.Contains(Name.Trim()))
                return $"{ScriptType} with same name already exist.";

            if (existingSolutionItemsName.Contains(Name.Trim()))
                return $"{ScriptType} with same name already exist.";

            if (Name.Length < 3)
                return "Name should be at least 3 chars long";

            if (!Extentions.IsUserObjectNameValid(Name))
                return $"Invalid {ScriptType} name";

            if(Parameters.Any(p=> string.IsNullOrEmpty(p.Name.Trim())))
                return "Parameter name is required";

            var duplicateParameters = Parameters.GroupBy(p => p.Name.Trim()).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateParameters.Any())
            {
                return $"Duplicate {duplicateParameters.First()} parameter name";
            }

            return string.Empty;
        }

    }
}
