
namespace TradingClient.Interfaces
{
    public interface IApplicationCore
    {
        IDataManager DataManager { get; }

        IUIManager UIManager { get; }

        IViewFactory ViewFactory { get; }

        IScriptingNotificationManager ScriptingNotificationManager { get; }

        IPathManager PathManager { get; }

        IScriptingLogManager ScriptingLogManager { get; }

        IScriptingGenerator ScriptingGenerator { get; }

        ISerializer ProtoSerializer { get; }

        ISettings Settings { get; }
    }
}