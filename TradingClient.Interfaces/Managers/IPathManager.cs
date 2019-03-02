namespace TradingClient.Interfaces
{
    public interface IPathManager
    {
        string RootDirectory { get; }

        string WorkspaceDirectory { get; }

        string PortfolioDirectory { get; }

        string SymbolListDirectory { get; }

        string ExportedOrdersDirectory { get; }

        string ScriptingDirectory { get; }

        string IndicatorsDirectory { get; }

        string SignalsDirectory { get; }

        string DeployDirectory { get; }

        string DebugServicesFileName { get; }

        string SettingsFileName { get; }

        string GetDirectory4Signal(string user, string signalFullName);

        string GetDirectory4Strategy(string user, string portfolio, string strategy);

        void DeletePortfolioStrategySignalFolder(string user, string relativePath);
    }
}