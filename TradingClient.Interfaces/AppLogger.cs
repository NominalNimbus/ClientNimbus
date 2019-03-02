using NLog;
using System;

namespace TradingClient.Interfaces
{
    public static class AppLogger
    {
        private static Logger _logger;

        static AppLogger()
        {
            _logger = LogManager.GetCurrentClassLogger();
        }

        public static void Error(string message) =>
            _logger.Error(message);

        public static void Error(Exception ex, string message) =>
            _logger.Error(ex, message);

        public static void Info(string message) =>
            _logger.Info(message);

        public static void Info(Exception ex, string message) =>
            _logger.Info(ex, message);

        public static void Warn(string message) =>
            _logger.Warn(message);

        public static void Warn(Exception ex, string message) =>
            _logger.Warn(ex, message);

        public static void Fatal(string message) =>
            _logger.Fatal(message);

        public static void Fatal(Exception ex, string message) =>
            _logger.Fatal(ex, message);
    }
}
