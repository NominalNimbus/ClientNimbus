using System.Collections.Generic;
using TradingClient.Data.Contracts;

namespace TradingClient.Interfaces
{
    public interface IScriptingSettings
    {
        string Name { get; set; }

        string Description { get; set; }

        string Author { get; set; }

        string Link { get; set; }

        ScriptingType ScriptType { get; }

        IExpandableObservableCollection<IScriptingParameter> Parameters { get; }

        void RefreshParameters();

        string ValidateSettings(IEnumerable<string> existingItemsName, IEnumerable<string> existingSolutionItemsName);
    }
}