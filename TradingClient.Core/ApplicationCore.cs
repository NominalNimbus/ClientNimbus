using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TradingClient.Common;
using TradingClient.Data.Contracts;
using TradingClient.DataProvider;
using TradingClient.Interfaces;

namespace TradingClient.Core
{
    public class ApplicationCore : IApplicationCore
    {
     
        #region Constructor

        public ApplicationCore()
        {
            UIManager = new TradingClient.UIManager.UIManager();
            UIManager.RegisterViews();

            ProtoSerializer = new Serializer();
            PathManager = new PathManager();
            LoadSettings();
            DataManager = new DataManager();
            ScriptingNotificationManager = new ScriptingNotificationManager();
            ScriptingLogManager = new ScriptingLogManager();
            ScriptingGenerator = new ScriptingGenerator(PathManager);
            SubscribeEvents();
        }

        #endregion // Constructor

        #region Properties

        public IDataManager DataManager { get; }

        public IUIManager UIManager { get; }

        public IViewFactory ViewFactory => UIManager.ViewFactory;

        public IScriptingNotificationManager ScriptingNotificationManager { get; }

        public IPathManager PathManager { get; }

        public IScriptingLogManager ScriptingLogManager { get; }

        public IScriptingGenerator ScriptingGenerator { get; }

        public ISerializer ProtoSerializer { get; }

        public ISettings Settings { get; private set; }

        #endregion // Properties

        #region Private

        private void SubscribeEvents()
        {
            DataManager.ScriptingManager.ScriptingDLLsReceived += OnScriptinhDLLsReceived;
            DataManager.ScriptingManager.ScriptingNotification += (s, e) => ScriptingNotificationManager.ExecuteNotification(e.Value1, e.Value2);
            DataManager.ScriptingManager.SignalFilesReceived += ScriptingSignalFilesReceived;
            DataManager.ScriptingManager.ScriptingLog += OnScriptingLog;
        }

        private void ScriptingSignalFilesReceived(object sender, EventArgs<Dictionary<string, byte[]>> e)
        {
            var files = e.Value;
            if (files == null)
                return;

            var root = Path.Combine(PathManager.PortfolioDirectory, Settings.UserName);
            FileManager.CreateDirectory(root);

            //check/compare signal directories
            var serverDirs = files.Keys.Select(i => i.Remove(i.LastIndexOf('\\'))).Distinct().ToList();
            var localDirs = new List<string>();
            foreach (var portfolioDir in Directory.GetDirectories(root))
                foreach (var strategyDir in Directory.GetDirectories(portfolioDir))
                    foreach (var signalDir in Directory.GetDirectories(strategyDir))
                        localDirs.Add(signalDir.Substring(root.Length + 1));

            //prompt user to delete directories that do not exist on server
            var dirsToDelete = localDirs.Where(i => !serverDirs.Contains(i)).ToList();
            if (dirsToDelete.Any())
            {
                string dirNames = String.Join(Environment.NewLine, dirsToDelete);
                string msg = dirsToDelete.Count == 1
                    ? String.Format("Following signal does not exist on server side: {0}{0}{1}{0}{0}Remove its settings?",
                        Environment.NewLine, dirNames)
                    : String.Format("Following signals do not exist on server side: {0}{0}{1}{0}{0}Remove their settings?",
                        Environment.NewLine, dirNames);
                if (ViewFactory.ShowMessage(msg, "Question", MsgBoxButton.YesNo, MsgBoxIcon.Question) == DlgResult.Yes)
                {
                    foreach (var dir in dirsToDelete)
                    {
                        try { Directory.Delete(dir.StartsWith(root) ? dir : Path.Combine(root, dir), true); }
                        catch { }
                    }
                }
            }

            //(re)write received files
            try
            {
                foreach (var item in files)
                    Extentions.UnzipData(item.Value, Path.Combine(root, item.Key));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Failed to unzip signal data: " + ex.Message);
            }
        }

        #endregion // Private

        #region Public

        public void UnLoad()
        {
            SaveSettings();
            DataManager.Logout();
        }

        #endregion // Public

        #region Scripting

        private void HandeScriptingDll(ScriptingDLLs scriptingDlls, StringBuilder stringBuilder)
        {
            var scriptingDllPath = Path.Combine(PathManager.DeployDirectory, "Scripting.dll");
            var scriptingDllVersion = Extentions.GetFileVersion(scriptingDllPath);
            if (scriptingDllVersion != scriptingDlls.ScriptingDllVersion && !string.IsNullOrEmpty(scriptingDlls.ScriptingDllVersion))
            {
                if (scriptingDlls.ScriptingDll.Length > 0)
                {
                    try
                    {
                        var data = Extentions.Decompress(scriptingDlls.ScriptingDll);
                        File.WriteAllBytes(scriptingDllPath, data);
                        stringBuilder.Append("Updated Scripting library to v" + scriptingDlls.ScriptingDllVersion);
                    }
                    catch (Exception ex)
                    {
                        ScriptingLogManager.SendLogMessage(ex.ToString());
                    }
                }
            }
        }

        private void HandleCommonObjectDll(ScriptingDLLs scriptingDlls, StringBuilder stringBuilder)
        {
            var commonObjectsDllPath = Path.Combine(PathManager.DeployDirectory, "CommonObjects.dll");
            var commonObjDllVersion = Extentions.GetFileVersion(commonObjectsDllPath);
            if (commonObjDllVersion != scriptingDlls.CommonObjectsDllVersion && !string.IsNullOrEmpty(scriptingDlls.CommonObjectsDllVersion))
            {
                if (scriptingDlls.CommonObjectsDll.Length > 0)
                {
                    try
                    {
                        var data = Extentions.Decompress(scriptingDlls.CommonObjectsDll);
                        File.WriteAllBytes(commonObjectsDllPath, data);
                        if (stringBuilder.Length > 0)
                            stringBuilder.Append(Environment.NewLine);

                        stringBuilder.Append("Updated common objects library to v" + scriptingDlls.CommonObjectsDllVersion);
                    }
                    catch (Exception ex)
                    {
                        ScriptingLogManager.SendLogMessage(ex.ToString());
                    }
                }
            }
        }

        private void HandleBackTestDll(ScriptingDLLs scriptingDlls, StringBuilder stringBuilder)
        {
            var backtesterDllPath = Path.Combine(PathManager.DeployDirectory, "Backtest.dll");
            var backtesterDllVersion = Extentions.GetFileVersion(backtesterDllPath);
            if (backtesterDllVersion != scriptingDlls.BacktesterDllVersion && !string.IsNullOrEmpty(scriptingDlls.BacktesterDllVersion))
            {
                if (scriptingDlls.BacktesterDll.Length > 0)
                {
                    try
                    {
                        var data = Extentions.Decompress(scriptingDlls.BacktesterDll);
                        File.WriteAllBytes(backtesterDllPath, data);
                        if (stringBuilder.Length > 0)
                            stringBuilder.Append(Environment.NewLine);

                        stringBuilder.Append("Updated backtester library to v" + scriptingDlls.BacktesterDllVersion);
                    }
                    catch (Exception ex)
                    {
                        ScriptingLogManager.SendLogMessage(ex.ToString());
                    }
                }
            }
        }

        #endregion // Scripting

        #region Event Handlers

        private void OnScriptingLog(object sender, ScriptingLogEventArgs args)
        {
            foreach (var output in args.Value)
                ScriptingLogManager?.SendLogMessage(output);
        }

        private void OnScriptinhDLLsReceived(object sender, ScriptingDLLs e)
        {
            var stringBuilder = new StringBuilder();
            HandeScriptingDll(e, stringBuilder);
            HandleCommonObjectDll(e, stringBuilder);
            HandleBackTestDll(e, stringBuilder);
            if (stringBuilder.Length > 0)
                ViewFactory.ShowMessage(stringBuilder.ToString());
        }

        #endregion // Event Handlers

        #region Settings

        private void LoadSettings()
        {
            if (!File.Exists(PathManager.SettingsFileName))
            {
                Settings = new Settings();
            }
            else
            {
                try
                {
                    var data = File.ReadAllBytes(PathManager.SettingsFileName);
                    Settings = ProtoSerializer.Deserialize<Settings>(data) ?? new Settings();
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Failed load settings.");
                }
            }
        }

        private void SaveSettings()
        {
            try
            {
                var brokerAccount = DataManager.Broker.ActiveAccounts.FirstOrDefault(p => p.IsDefault);
                if (brokerAccount != null)
                {
                    Settings.DefaultBrokerAccount = brokerAccount.UserName;
                    Settings.DefaultBrokerName = brokerAccount.BrokerName;
                }
                else
                {
                    Settings.DefaultBrokerAccount = string.Empty;
                    Settings.DefaultBrokerName = string.Empty;
                }

                Settings.AutoLoginBrokerAccounts = true;
                var data = ProtoSerializer.Serialize(Settings);
                File.WriteAllBytes(PathManager.SettingsFileName, data);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Failed save settings");
            }
        }

        #endregion // Settings
    }
}