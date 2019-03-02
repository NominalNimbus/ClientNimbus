using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.Linq;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;

namespace TradingClient.Common
{
    public class IndicatorSettings : ScriptingSettingsBase, IIndicatorSettings
    {
        private IExpandableObservableCollection<ISeriesSettings> _series;

        [Browsable(false)]
        public override ScriptingType ScriptType => ScriptingType.Indicator;

        [ExpandableObject]
        [Display(Name = "Series", Order = 60)]
        [RefreshProperties(RefreshProperties.All)]
        public IExpandableObservableCollection<ISeriesSettings> Series
        {
            get => _series;
            set => SetPropertyValue(ref _series, value, nameof(Series));
        }

        public IndicatorSettings() : base()
        {
            Series = new ExpandableObservableCollection<ISeriesSettings>();
        }

        public void RefreshSeries() =>
         Series = new ExpandableObservableCollection<ISeriesSettings>(Series);

        public override string ValidateSettings(IEnumerable<string> existingItemsName, IEnumerable<string> existingSolutionItemsName)
        {
            var error = base.ValidateSettings(existingItemsName, existingSolutionItemsName);
            if (!string.IsNullOrEmpty(error))
                return error;


            if (Series.Any(p => string.IsNullOrEmpty(p.Name.Trim())))
                return "Series name is required";

            var duplicateSeries = Series.GroupBy(p => p.Name.Trim()).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateSeries.Any())
            {
                return $"Duplicate {duplicateSeries.First()} series name";
            }

            return string.Empty;
        }
    }

}