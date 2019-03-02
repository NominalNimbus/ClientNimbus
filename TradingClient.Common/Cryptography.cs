using System;
using System.Security.Cryptography;
using System.Text;

namespace TradingClient.Common
{
    public static class Cryptography
    {
        private const string DEFAULT_KEY = "-Sup3rDup3rMe94K3Y-";

        /// <summary>
        /// Encrypts a string using .NET AES encryption
        /// </summary>
        /// <param name="message">String message to be encrypted</param>
        /// <param name="key">Key to bee used during encryption</param>
        /// <returns>AES encrypted base64 string</returns>
        public static string Encrypt(string message, string key = null)
        {
            if (String.IsNullOrEmpty(message))
                return message;

            try
            {
                return Convert.ToBase64String(EncryptBytes(Encoding.ASCII.GetBytes(message),
                    String.IsNullOrWhiteSpace(key) ? DEFAULT_KEY : key, true));
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError("Failed to encrypt string: " + e.Message);
                return String.Empty;
            }
        }

        /// <summary>
        /// Decrypts a string using .NET AES implementation
        /// </summary>
        /// <param name="encrypted">Encrypted base64 string message</param>
        /// <param name="key">Key for decryption</param>
        /// <returns>Decrypted message (or null on failure)</returns>
        public static string Decrypt(string encrypted, string key = null)
        {
            if (String.IsNullOrEmpty(encrypted))
                return encrypted;

            try
            {
                return Encoding.ASCII.GetString(EncryptBytes(Convert.FromBase64String(encrypted), 
                    String.IsNullOrWhiteSpace(key) ? DEFAULT_KEY : key, false));
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError("Failed to decrypt string: " + e.Message);
                return String.Empty;
            }
        }

        private static byte[] EncryptBytes(byte[] input, string password, bool encrypt)
        {
            ICryptoTransform transform = null;
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.Padding = PaddingMode.PKCS7;

                var derivedBytes = new Rfc2898DeriveBytes(password,
                    Convert.FromBase64String("ACARQic6TFfG8fDuIsI0RQ=="), 10000);
                var key = derivedBytes.GetBytes(aes.LegalKeySizes[0].MaxSize / 8);
                var iv = derivedBytes.GetBytes(aes.BlockSize / 8);
                transform = encrypt ? aes.CreateEncryptor(key, iv) : aes.CreateDecryptor(key, iv);
            }

            using (var output = new System.IO.MemoryStream())
            using (var stream = new CryptoStream(output, transform, CryptoStreamMode.Write))
            {
                stream.Write(input, 0, input.Length);
                stream.FlushFinalBlock();
                return output.ToArray();
            }
        }

    }
}
