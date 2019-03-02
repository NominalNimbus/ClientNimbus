
namespace TradingClient.Interfaces
{
    public interface IScriptingGenerator
    {
        string CreateIndicator(string path, IIndicatorSettings settings);

        string CreateSignal(string path, ISignalSettings settings);
    }
}
