using ProtoBuf;
using TradingClient.Interfaces;

namespace TradingClient.ViewModels
{
    [ProtoContract]
    public class WorkspaceDocument
    {
        public WorkspaceDocument()
        {
            DocumentType = DocumentType.Unknown;
            SerializedData = new byte[0];
        }

        [ProtoMember(1)]
        public DocumentType DocumentType { get; set; }

        [ProtoMember(2)]
        public byte[] SerializedData { get; set; }
    }
}