using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TradingClient.Data.Contracts;

namespace TradingClient.Interfaces
{
    public interface IScriptingManager
    {
        event Action ScriptingListUpdated;
        event EventHandler<EventArgs<Indicator, string>> IndicatorInstanceAdded;
        event EventHandler<EventArgs<string>> IndicatorInstanceRemoved;
        event EventHandler<EventArgs<Signal>> SignalInstanceUpdated;
        event EventHandler<EventArgs<string>> SignalInstanceRemoved;
        event EventHandler<EventArgs<List<SeriesForUpdate>>> SeriesUpdated;
        event EventHandler<EventArgs<List<BacktestResult>>> SignalBacktestUpdated;
        event EventHandler<EventArgs<Dictionary<string, byte[]>>> SignalFilesReceived;
        event EventHandler<ScriptingLogEventArgs> ScriptingLog;
        event EventHandler<ScriptingDLLs> ScriptingDLLsReceived;
        event EventHandler<EventArgs<string>> ScriptingMessage;
        event EventHandler<EventArgs<string, List<string>>> ScriptingNotification;
        
        Dictionary<string, List<ScriptingParameterBase>> Indicators { get; }
        List<string> DefaultIndicators { get; }
        List<Signal> Signals { get; }

        void AddIndicator(IndicatorReqParams reqParams);
        void RemoveIndicator(string id);
        string SendIndicatorToServer(string solutionPath);
        void RemoveIndicatorFromServer(string name);

        Task AddSignal(SignalReqParams reqParams, string settingsPath = null, string solutionPath = null);
        string SendSignalToServer(string solutionPath, string settingsPath);
        void InvokeSignalAction(string name, SignalAction action);
        void GetReport(string signalName, DateTime fromTime, DateTime toTime, Action<IEnumerable<ReportField>> applyReport);
        void RemoveSignal(string id);
        void RemoveSignalFromServer(string name);
        void UpdateSignalStrategy(string name, StrategyParams parameters);
        void UploadFolderToServer(string userDir, string relativePath, bool skipDlls = false);
        void RequestBacktestResults(string fullName);
        void DeleteFilesOnServer(string relativePath);

        IEnumerable<string> GetScriptNamesFromDirectory(string directory);
    }
}
