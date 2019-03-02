using System;
using System.IO;

namespace TradingClient.Common
{
    public static class FileManager
    {
        #region Directory

        public static void DeleteDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path, true);
                }
                catch (Exception ex)
                {
                    //Log.Logger.Error(ex, $"Failed to delete directory: {path}");
                }
            }
        }

        public static void CreateDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception ex)
                {
                    //Log.Logger.Error(ex, $"Failed to create directory: {path}");
                }
            }
        }

        #endregion // Directory

        #region File

        public static void CreateFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            if (!File.Exists(path))
            {
                try
                {
                    var fileStream = File.Create(path);
                    fileStream.Close();
                }
                catch (Exception ex)
                {
                    //Log.Logger.Error(ex, $"Failed to create file: {path}");
                }
            }
        }

        #endregion // File
    }
}
