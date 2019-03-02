using System.Collections.Generic;
using ProtoBuf;

namespace TradingClient.ViewModels
{
    [ProtoContract]
    public class Workspace
    {
        public Workspace()
        {
            SerializedLayout = new byte[0];
            Documents = new Dictionary<string, WorkspaceDocument>();
        }

        [ProtoMember(1)]
        public byte[] SerializedLayout { get; set; }

        [ProtoMember(2)]
        public Dictionary<string, WorkspaceDocument> Documents { get; set; }
    }
}   