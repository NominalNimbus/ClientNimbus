namespace TradingClient.Interfaces
{
    public interface ISerializer
    {
        byte[] Serialize(object data);

        T Deserialize<T>(byte[] data);
    }
}