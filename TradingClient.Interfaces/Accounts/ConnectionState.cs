namespace TradingClient.Interfaces
{
    public enum ConnectionState
    {
        Connect,
        Disconnect,
        LostConnection,
        ServerDisconnected,
        DisconnectedByAnotherUser,
    }
}