using System;
using System.IO;
using TradingClient.Interfaces;

namespace TradingClient.Common
{
    public sealed class PathManager : IPathManager
    {
        public const string ProductName = "TradingClient";

        #region Constructor

        public PathManager()
        {
            InitDirectories();
            InitFiles();
        }

        #endregion // Constructor
        
        #region Properties

        public string RootDirectory { get; private set;}

        public string WorkspaceDirectory { get; private set;}

        public string PortfolioDirectory { get; private set;}

        public string SymbolListDirectory { get; private set;}

        public string ExportedOrdersDirectory { get; private set;}

        public string ScriptingDirectory { get; private set;}

        public string IndicatorsDirectory { get; private set;}

        public string SignalsDirectory { get; private set;}

        public string DeployDirectory { get; private set;}

        public string DebugServicesFileName { get; private set;}

        public string SettingsFileName { get; private set;}

        #endregion // Properties

        #region Private

        private void InitFiles()
        {
            SettingsFileName = Path.Combine(RootDirectory, "Settings.conf");
            DebugServicesFileName = Path.Combine("Debug service", "Scripting.zip");
        }

        private void InitDirectories()
        {
            RootDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ProductName);
            FileManager.CreateDirectory(RootDirectory);
            
            WorkspaceDirectory = Path.Combine(RootDirectory, "Workspaces");
            FileManager.CreateDirectory(WorkspaceDirectory);

            PortfolioDirectory = Path.Combine(RootDirectory, "Portfolios");
            FileManager.CreateDirectory(PortfolioDirectory);

            SymbolListDirectory = Path.Combine(RootDirectory, "Symbol List");
            FileManager.CreateDirectory(SymbolListDirectory);

            ExportedOrdersDirectory = Path.Combine(RootDirectory, "Exported Orders");
            FileManager.CreateDirectory(ExportedOrdersDirectory);

            ScriptingDirectory = Path.Combine(RootDirectory, "Scripting");
            FileManager.CreateDirectory(ScriptingDirectory);

            IndicatorsDirectory = Path.Combine(ScriptingDirectory, "Indicators");
            FileManager.CreateDirectory(IndicatorsDirectory);

            SignalsDirectory = Path.Combine(ScriptingDirectory, "Signals");
            FileManager.CreateDirectory(SignalsDirectory);

            DeployDirectory = Path.Combine(ScriptingDirectory, "Deploy");
            FileManager.CreateDirectory(DeployDirectory);
        }

        #endregion // Private

        #region IPathManager

        public string GetDirectory4Signal(string user, string signalName)
        {
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrEmpty(signalName))
                return string.Empty;

            return Path.Combine(PortfolioDirectory, user, signalName);
        }

        public string GetDirectory4Strategy(string user, string portfolio, string strategy)
        {
            var isUserEmpty = string.IsNullOrWhiteSpace(user);
            var isPortfolioEmpty = string.IsNullOrWhiteSpace(portfolio);
            var isStrategyEmpty = string.IsNullOrWhiteSpace(strategy);

            return isUserEmpty || isPortfolioEmpty || isStrategyEmpty ? 
                string.Empty : Path.Combine(PortfolioDirectory, user, portfolio, strategy);
        }

        public void DeletePortfolioStrategySignalFolder(string user, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(relativePath))
                return;

            var path = Path.Combine(PortfolioDirectory, user, relativePath);
            FileManager.DeleteDirectory(path);
        }

        #endregion // IPathManager
    }
}