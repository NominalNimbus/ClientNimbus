using System;
using System.IO;
using System.Windows.Media;
using ProtoBuf.Meta;
using TradingClient.Interfaces;

namespace TradingClient.Common
{
    public class Serializer : ISerializer
    {
        #region Constructor

        public Serializer()
        {
            RuntimeTypeModel.Default.CompileInPlace();
        }

        #endregion //Constructor

        #region ISerializer

        public byte[] Serialize(object data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            try
            {
                using (var stream = new MemoryStream())
                {
                    ProtoBuf.Serializer.Serialize(stream, data);
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Serialized object failed.");
            }

            return null;
        }

        public T Deserialize<T>(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            try
            {
                using (var stream = new MemoryStream(data))
                {
                    return ProtoBuf.Serializer.Deserialize<T>(stream);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Deserialized object failed.");
            }

            return default(T);
        }

        #endregion // ISerializer
    }
}