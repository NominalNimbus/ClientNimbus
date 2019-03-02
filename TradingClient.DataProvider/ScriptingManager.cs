using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradingClient.DataProvider.TradingService;
using ScriptingParameterBase = TradingClient.Data.Contracts.ScriptingParameterBase;
using Signal = TradingClient.Data.Contracts.Signal;
using Indicator = TradingClient.Data.Contracts.Indicator;
using ReportField = TradingClient.Data.Contracts.ReportField;
using SeriesForUpdate = TradingClient.Data.Contracts.SeriesForUpdate;
using SignalAction = TradingClient.Data.Contracts.SignalAction;
using StrategyParams = TradingClient.Data.Contracts.StrategyParams;
using ScriptingType = TradingClient.DataProvider.TradingService.ScriptingType;
using TradingClient.Interfaces;
using TradingClient.Data.Contracts;

namespace TradingClient.DataProvider
{
    public class ScriptingManager : IScriptingManager
    {
        private readonly ServiceConnector _connector;
        public event Action ScriptingListUpdated;
        public event EventHandler<EventArgs<Indicator, string>> IndicatorInstanceAdded;
        public event EventHandler<EventArgs<string>> IndicatorInstanceRemoved;
        public event EventHandler<EventArgs<List<SeriesForUpdate>>> SeriesUpdated;
        public event EventHandler<EventArgs<Signal>> SignalInstanceUpdated;
        public event EventHandler<EventArgs<List<BacktestResult>>> SignalBacktestUpdated;
        public event EventHandler<EventArgs<string>> SignalInstanceRemoved;

        public event EventHandler<ScriptingDLLs> ScriptingDLLsReceived
        {
            add { _connector.ScriptingDLLsReceived += value; }
            remove { _connector.ScriptingDLLsReceived -= value; }
        }
        public event EventHandler<EventArgs<string>> ScriptingMessage
        {
            add { _connector.ScriptingMessage += value; }
            remove { _connector.ScriptingMessage -= value; }
        }
        public event EventHandler<EventArgs<string, List<string>>> ScriptingNotification
        {
            add { _connector.ScriptingAlert += value; }
            remove { _connector.ScriptingAlert -= value; }
        }
        public event EventHandler<EventArgs<Dictionary<string, byte[]>>> SignalFilesReceived
        {
            add { _connector.SignalFilesReceived += value; }
            remove { _connector.SignalFilesReceived -= value; }
        }
        public event EventHandler<ScriptingLogEventArgs> ScriptingLog
        {
            add { _connector.ScriptingLog += value; }
            remove { _connector.ScriptingLog -= value; }
        }

        public Dictionary<string, List<ScriptingParameterBase>> Indicators { get; private set; }
        public List<string> DefaultIndicators { get; private set; }
        public List<Signal> Signals { get; private set; }

        private readonly Dictionary<string, Action<IEnumerable<ReportField>>> _reportActions;

        public ScriptingManager(ServiceConnector connector)
        {
            _connector = connector;
            Indicators = new Dictionary<string, List<ScriptingParameterBase>>();
            DefaultIndicators = new List<string>();
            Signals = new List<Signal>();

            _connector.ScriptingListReceived += ConnectorOnScriptingListReceived;
            _connector.IndicatorInstanceAdded += ConnectorOnIndicatorInstanceAdded;
            _connector.SignalInstanceAdded += ConnectorOnSignalInstanceAdded;
            _connector.WorkingSignalInstanceReceived += ConnectorOnWorkingSignalInstanceReceived;
            _connector.SeriesUpdated += ConnectorOnSeriesUpdated;
            _connector.ScriptingSaved += ConnectorOnScriptingSaved;
            _connector.ScriptingUnloaded += ConnectorOnScriptingUnloaded;
            _connector.ScriptingExit += ConnectorOnScriptingExit;
            _connector.SignalActionSet += ConnectorOnSignalActionSet;
            _connector.BacktestProgressUpdated += ConnectorOnBacktestProgressUpdated;
            _connector.ScriptingReport += ConnectorOnScriptingReport;

            _reportActions = new Dictionary<string, Action<IEnumerable<ReportField>>>();
        }

        public void AddIndicator(IndicatorReqParams reqPrams)
        {
            ThreadPool.QueueUserWorkItem(p => _connector.Send(new CreateUserIndicatorRequest
            {
                Name = reqPrams.Name,
                PriceType = DataConverter.ToDsPriceType(reqPrams.PriceType),
                Parameters = reqPrams.Parameters.Select(DataConverter.ToDsScriptingParameters).ToList(),
                RequestID = reqPrams.ID,
                Selection = DataConverter.ToDsSelection(reqPrams)
            }));
        }

        public void RemoveIndicator(string name)
        {
            ThreadPool.QueueUserWorkItem(p => _connector.Send(new RemoveScriptingInstanceRequest
            {
                Name = name,
                ScriptingType = ScriptingType.Indicator,
                RemoveProject = false
            }));
        }

        public string SendIndicatorToServer(string solutionPath)
        {
            var indicatorName = Path.GetFileNameWithoutExtension(solutionPath);
            var DLLs = GetCompressedScriptingDlls(Path.GetDirectoryName(solutionPath));
            if (!DLLs.ContainsKey(indicatorName + ".dll"))
                return $"Failed to locate or compress {indicatorName} libraries";

            if (Indicators.ContainsKey(indicatorName))
            {
                Indicators.Remove(indicatorName);
                ScriptingListUpdated?.Invoke();
                IndicatorInstanceRemoved?.Invoke(this, new EventArgs<string>(indicatorName));
            }

            _connector.Send(new SaveScriptingDataRequest
            {
                Files = DLLs,
                Path = indicatorName,
                RequestID = Guid.NewGuid().ToString(),
                ScriptingType = ScriptingType.Indicator
            });

            return String.Empty;
        }

        public void RemoveIndicatorFromServer(string name)
        {
            Indicators.Remove(name);
            _connector.Send(new RemoveScriptingInstanceRequest
            {
                Name = name,
                ScriptingType = ScriptingType.Indicator,
                RemoveProject = true
            });

            ScriptingListUpdated?.Invoke();
            IndicatorInstanceRemoved?.Invoke(this, new EventArgs<string>(name));
        }

        public async Task AddSignal(SignalReqParams reqParams, string settingsPath = null, string solutionPath = null)  //initializes and spins the Signal
        {
            //optional: add signal files to re-deploy on server
            Dictionary<string, byte[]> files = null;
            if (Directory.Exists(settingsPath) && Directory.Exists(solutionPath))
            {
                CopySignalDllsToSettingsDir(solutionPath, settingsPath);
            }
            if(Directory.Exists(settingsPath))
            {
                files = GetCompressedSignalDataFiles(settingsPath);
            }

            await Task.Run(() =>
            {
                var createUserSignalRequest = new CreateUserSignalRequest
                {
                    SignalName = reqParams.FullName,
                    Files = files,
                    StrategyParameters = DataConverter.ToDsStrategyParams(reqParams.StrategyParameters),
                    BacktestSettings =
                        DataConverter.ToDsBacktestSettings(reqParams.BacktestSettings,
                            reqParams.StrategyBacktestSettings),
                    Parameters = reqParams.Parameters.Select(DataConverter.ToDsScriptingParameters).ToList(),
                    Selections = reqParams.Selections.Select(i => DataConverter.ToDsSelection(i)).ToList(),
                    InitialState = (reqParams.BacktestSettings != null && reqParams.StrategyBacktestSettings != null)
                        ? SignalState.Backtesting
                        : (reqParams.IsSimulated ? SignalState.RunningSimulated : SignalState.Running),
                    AccountInfos = reqParams.Accounts.Select(DataConverter.ToServerPortfolioAccount).ToList()
                };

                _connector.Send(createUserSignalRequest);
            });
        }

        public string SendSignalToServer(string solutionPath, string settingsPath)  //saves Signal on server (does not start)
        {
            //if (!Directory.Exists(solutionPath))
            //    return "Solution directory does not exist";

            string signalRelativePath;
            var levels = settingsPath.Split(Path.DirectorySeparatorChar);
            if (levels.Length > 3)
                signalRelativePath = String.Join("\\", levels.Skip(levels.Length - 3));  //portfolio/strategy/signal
            else
                return "Invalid signal settings path";

            CopySignalDllsToSettingsDir(solutionPath, settingsPath);
            var files = GetCompressedSignalDataFiles(settingsPath);

            var name = signalRelativePath.Substring(signalRelativePath.LastIndexOf('\\') + 1);
            if (!files.Any(i => i.Key.EndsWith(name + ".dll")))
                return "Signal DLL does not exist";

            if (!String.IsNullOrEmpty(signalRelativePath) && Signals.Any(i => i.FullName == signalRelativePath))
            {
                Signals.Remove(Signals.First(i => i.FullName == signalRelativePath));
                SignalInstanceRemoved?.Invoke(this, new EventArgs<string>(signalRelativePath));
                ScriptingListUpdated?.Invoke();
            }

            _connector.Send(new SaveScriptingDataRequest
            {
                Files = files,
                Path = signalRelativePath,
                RequestID = Guid.NewGuid().ToString(),
                ScriptingType = ScriptingType.Signal
            });

            return null;
        }

        public void InvokeSignalAction(string fullName, SignalAction action)
        {
            _connector.Send(new SignalActionRequest
            {
                SignalName = fullName,
                Action = DataConverter.ToDsSignalAction(action)
            });
        }

        public void GetReport(string signalName, DateTime fromTime, DateTime toTime, Action<IEnumerable<ReportField>> applyReport)
        {
            var id = Guid.NewGuid().ToString();
            _connector.Send(new ScriptingReportRequest
            {
                Id = id,
                SignalName = signalName,
                FromTime = fromTime,
                ToTime = toTime
            });

            _reportActions.Add(id, applyReport);
        }

        public void RemoveSignal(string fullName)  //unloads (stops) Signal on server (but keeps the solution)
        {
            _connector.Send(new RemoveScriptingInstanceRequest
            {
                Name = fullName,
                ScriptingType = ScriptingType.Signal,
                RemoveProject = false
            });

            Signals.RemoveAll(i => i.FullName == fullName);
        }

        public void RemoveSignalFromServer(string fullName)  //removes Signal solution and data from server
        {
            Signals.Remove(Signals.FirstOrDefault(i => i.FullName == fullName));
            _connector.Send(new RemoveScriptingInstanceRequest
            {
                Name = fullName,
                ScriptingType = ScriptingType.Signal,
                RemoveProject = true
            });

            SignalInstanceRemoved?.Invoke(this, new EventArgs<string>(fullName));
            ScriptingListUpdated?.Invoke();
        }

        public void UpdateSignalStrategy(string fullName, StrategyParams parameters)
        {
            _connector.Send(new UpdateStrategyParamsRequest
            {
                SignalName = fullName,
                Parameters = DataConverter.ToDsStrategyParams(parameters)
            });
        }

        public void RequestBacktestResults(string fullName)
        {
            _connector.Send(new BacktestResultsRequest { SignalName = fullName });
        }

        public void UploadFolderToServer(string userDir, string folder, bool skipDlls = false)
        {
            var path = Path.Combine(userDir, folder);
            if (Directory.Exists(path))
            {
                var zip = Extentions.ZipFolder(path, skipDlls ? null : "dll");
                _connector.Send(new AddUserFilesRequest { Path = folder, ZippedFiles = zip });
            }
        }

        public void DeleteFilesOnServer(string relativePath)
        {
            if (!String.IsNullOrWhiteSpace(relativePath) && !relativePath.Contains(':'))
                _connector.Send(new RemoveUserFilesRequest { Paths = new List<string> { relativePath } });
        }

        public IEnumerable<string> GetScriptNamesFromDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                var directories = Directory.GetDirectories(directory);
                foreach (var dir in directories)
                {
                    foreach (var item in Directory.GetFiles(dir, "*.sln", SearchOption.TopDirectoryOnly))
                        yield return Path.GetFileNameWithoutExtension(item);
                }
            }
        }

        #region Private methods

        private void ConnectorOnScriptingListReceived(object sender, EventArgs<ScriptingReceivedEventArgs> args)
        {
            lock (Indicators)
            {
                Indicators.Clear();

                foreach (var item in args.Value.Indicators.ToList())
                    Indicators.Add(item.Key, item.Value);

                DefaultIndicators.Clear();
                DefaultIndicators.AddRange(args.Value.DefaultIndicators.ToList());
            }

            lock (Signals)
            {
                Signals.Clear();
                foreach (var item in args.Value.Signals)
                    Signals.Add(item);
            }

            ScriptingListUpdated?.Invoke();
        }

        private void ConnectorOnScriptingSaved(object sender, EventArgs<ScriptingSavedEventArgs> eventArgs)
        {
            if (eventArgs.Value.ScriptingType == Data.Contracts.ScriptingType.Indicator)
            {
                lock (Indicators)
                {
                    if (Indicators.ContainsKey(eventArgs.Value.Name))
                        Indicators.Remove(eventArgs.Value.Name);

                    Indicators.Add(eventArgs.Value.Name, eventArgs.Value.Parameters);
                }
            }
            else if (eventArgs.Value.ScriptingType == Data.Contracts.ScriptingType.Signal)
            {
                lock (Signals)
                {
                    var signal = Signals.FirstOrDefault(i => i.FullName == eventArgs.Value.Name);
                    if (signal != null)
                    {
                        signal.Parameters = new List<ScriptingParameterBase>(eventArgs.Value.Parameters);
                    }
                    else
                    {
                        signal = new Signal
                        {
                            FullName = eventArgs.Value.Name,
                            Parameters = new List<ScriptingParameterBase>(eventArgs.Value.Parameters)
                        };
                    }

                    if (signal.State == State.New)
                    {
                        signal.State = State.Stopped;
                        signal.SetDefaultParamSpace();
                    }

                    if (Signals.All(i => i.FullName != signal.FullName))
                        Signals.Add(signal);
                }
            }

            ScriptingListUpdated?.Invoke();
        }

        private void ConnectorOnScriptingExit(object sender, EventArgs<string, ScriptingType> e)
        {
            if (e.Value2 != ScriptingType.Signal)
                return;  //or throw new NotImplementedException($"ScriptingExit for {e.Value2} is not implemented");

            var signal = Signals.FirstOrDefault(i => i.FullName == e.Value1);
            if (signal != null && SignalInstanceUpdated != null)
            {
                signal.State = State.Stopped;
                SignalInstanceUpdated(this, new EventArgs<Signal>(signal));
            }
        }

        private void ConnectorOnScriptingUnloaded(object sender, EventArgs<string, ScriptingType> e)
        {
            bool updated = false;
            if (e.Value2 == ScriptingType.Indicator)
            {
                lock (Indicators)
                {
                    if (Indicators.ContainsKey(e.Value1))
                    {
                        Indicators.Remove(e.Value1);
                        updated = true;
                    }
                }
            }
            else if (e.Value2 == ScriptingType.Signal)
            {
                lock (Signals)
                {
                    var signal = Signals.FirstOrDefault(i => i.FullName == e.Value1);
                    if (signal != null)
                    {
                        signal.State = State.Stopped;
                        Signals.Remove(signal);
                        updated = true;
                    }
                }
            }

            if (updated && ScriptingListUpdated != null)
                ScriptingListUpdated();
        }

        private void ConnectorOnIndicatorInstanceAdded(object sender, EventArgs<string, Indicator> eventArgs)
        {
            IndicatorInstanceAdded?.Invoke(this, new EventArgs<Indicator, string>(eventArgs.Value2, eventArgs.Value1));
        }

        private void ConnectorOnSignalInstanceAdded(object sender, EventArgs<string, Signal> eventArgs)
        {
            var item = Signals.FirstOrDefault(i => i.FullName == eventArgs.Value2.FullName);
            if (item == null)
                Signals.Add(eventArgs.Value2);
            else if (item.State != eventArgs.Value2.State)
                item.State = eventArgs.Value2.State;

            SignalInstanceUpdated?.Invoke(this, new EventArgs<Signal>(eventArgs.Value2));
        }

        private void ConnectorOnWorkingSignalInstanceReceived(object sender, EventArgs<Signal> eventArgs)
        {
            var item = Signals.FirstOrDefault(i => i.FullName == eventArgs.Value.FullName);
            if (item == null)
                Signals.Add(eventArgs.Value);
            else if (item.State != eventArgs.Value.State)
                item.State = eventArgs.Value.State;

            SignalInstanceUpdated?.Invoke(this, new EventArgs<Signal>(eventArgs.Value));
        }

        private void ConnectorOnSignalActionSet(object sender, EventArgs<SignalActionResponse> e)
        {
            var item = Signals.FirstOrDefault(i => i.FullName == e.Value.SignalName);
            if (item != null)
            {
                item.State = DataConverter.ToSignalState(e.Value.State);
                SignalInstanceUpdated?.Invoke(this, new EventArgs<Signal>(item));
            }

            if (!String.IsNullOrEmpty(e.Value.Error))
                System.Diagnostics.Trace.TraceError("Failed to invoke signal action: " + e.Value.Error);
        }

        private void ConnectorOnBacktestProgressUpdated(object sender, EventArgs<BacktestReportMessage> e)
        {
            if (e?.Value?.BacktestResults == null)
                return;

            foreach (var item in e.Value.BacktestResults)
            {
                var signal = Signals.FirstOrDefault(i => i.FullName == item.SignalName);
                if (signal == null)
                    continue;

                SignalBacktestUpdated?.Invoke(this,
                    new EventArgs<List<BacktestResult>>(DataConverter.ToClientBacktestResults(item)));

                if (signal.IsBacktesting)
                {
                    if (item.TotalProgress >= 100F)
                    {
                        //RemoveSignal(signal.FullName);  //optional: auto-unload signal when backtest is finished
                        signal.State = State.Stopped;
                    }
                    else
                    {
                        signal.BacktestProgress = item.TotalProgress;
                    }
                }

                SignalInstanceUpdated?.Invoke(this, new EventArgs<Signal>(signal));
            }
        }
        private void ConnectorOnScriptingReport(object sender, ReportEventArgs eventArgs)
        {
            if (_reportActions.TryGetValue(eventArgs.Id, out var action))
                action(eventArgs.ReportFields);
        }

        private void ConnectorOnSeriesUpdated(object sender, EventArgs<List<SeriesForUpdate>> eventArgs)
        {
            if (SeriesUpdated != null && eventArgs.Value.Any())
                SeriesUpdated(this, new EventArgs<List<SeriesForUpdate>>(eventArgs.Value));
        }

        private static void CopySignalDllsToSettingsDir(string solutionPath, string settingsPath)
        {
            var bins = Environment.Is64BitProcess
                ? Path.Combine(solutionPath, "SimulatedServer", "bin", "x64", "Release")
                : Path.Combine(solutionPath, "SimulatedServer", "bin", "Release");
            if (!Directory.Exists(bins))
                return;

            var sharedDlls = new List<string>
            {
                "CommonObjects",
                "DebugService",
                "Backtest",
                "Scripting",
                "Xceed.Wpf.Toolkit"
            };
            var dlls = Directory.GetFiles(bins, "*.dll", SearchOption.TopDirectoryOnly)
                .Where(i => !sharedDlls.Contains(Path.GetFileNameWithoutExtension(i)));
            if (!dlls.Any())
                return;

            try
            {
                if (!Directory.Exists(settingsPath))
                    Directory.CreateDirectory(settingsPath);
                foreach (var dll in dlls)
                    File.Copy(dll, Path.Combine(settingsPath, Path.GetFileName(dll)), true);
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError("Failed to copy signal DLLs: " + e.Message);
            }
        }

        private static Dictionary<string, byte[]> GetCompressedScriptingDlls(string solutionPath)
        {
            var bins = Path.Combine(solutionPath, "SimulatedServer", "bin", "x64", "Release");  //x64 first
            if (!Directory.Exists(bins))
                bins = Path.Combine(solutionPath, "SimulatedServer", "bin", "Release");  //fallback to x86
            if (!Directory.Exists(bins))
                return new Dictionary<string, byte[]>(0);

            var sharedDlls = new List<string>
            {
                "CommonObjects",
                "DebugService",
                "Backtest",
                "Scripting",
                "Xceed.Wpf.Toolkit"
            };

            var result = new Dictionary<string, byte[]>();
            foreach (var dll in Directory.GetFiles(bins, "*.dll", SearchOption.TopDirectoryOnly))
            {
                if (!sharedDlls.Contains(Path.GetFileNameWithoutExtension(dll)))
                    result.Add(Path.GetFileName(dll), Extentions.Compress(File.ReadAllBytes(dll)));
            }
            return result;
        }

        private static Dictionary<string, byte[]> GetCompressedSignalDataFiles(string signalDataPath)
        {
            if (!Directory.Exists(signalDataPath))
                return new Dictionary<string, byte[]>(0);

            var path = signalDataPath;
            var result = new Dictionary<string, byte[]>();
            for (int i = 4; i > 1; i--)  //scan signal, strategy and portfolio directory levels
            {
                foreach (var file in Directory.GetFiles(path))
                {
                    //do not upload backtest results back to server
                    if (Path.GetFileName(file) == "Backtest Results.xml")
                        continue;

                    var levels = file.Split(Path.DirectorySeparatorChar);
                    levels = levels.Skip(levels.Length - i).ToArray();  //path relative to portfolio
                    result.Add(String.Join("\\", levels), Extentions.Compress(File.ReadAllBytes(file)));
                }
                path = Directory.GetParent(path).FullName;
            }
            return result;
        }

        #endregion
    }
}
