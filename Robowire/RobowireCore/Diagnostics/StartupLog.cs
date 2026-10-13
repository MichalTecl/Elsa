using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Robowire.Diagnostics
{
    /// <summary>
    /// Minimal startup timing log, independent of the application logging.
    /// Writes "timestamp\taction" lines to C:\Elsa\Log\Startup\{application}_{startDt}.log
    /// </summary>
    public static class StartupLog
    {
        private const string LOG_DIRECTORY = @"C:\Elsa\Log\Startup";
        private const string TIMESTAMP_FORMAT = "yyyy-MM-dd HH:mm:ss.fff";

        private static readonly object _lock = new object();

        private static string _applicationName;
        private static string _logFile;

        /// <summary>
        /// Sets the name used in the log file name. Needed where there is no entry assembly (ASP.NET);
        /// otherwise the entry assembly name is used. Has to be called before the first Write.
        /// </summary>
        public static void SetApplicationName(string applicationName)
        {
            lock (_lock)
            {
                if (_logFile == null)
                {
                    _applicationName = applicationName;
                }
            }
        }

        public static void Write(string action)
        {
            try
            {
                lock (_lock)
                {
                    if (_logFile == null)
                    {
                        Initialize();
                    }

                    File.AppendAllText(_logFile, $"{DateTime.Now.ToString(TIMESTAMP_FORMAT)}\t{action}\r\n");
                }
            }
            catch (Exception ex)
            {
                // Startup log must never break the application
                TryWriteError(action, ex);
            }
        }

        private static void TryWriteError(string action, Exception ex)
        {
            try
            {
                File.AppendAllText(
                    Path.Combine(Path.GetTempPath(), "Robowire_StartupLog_errors.log"),
                    $"{DateTime.Now.ToString(TIMESTAMP_FORMAT)}\t{action}\t{ex}\r\n");
            }
            catch (Exception)
            {
                // Nothing more to do
            }
        }

        private static void Initialize()
        {
            var applicationName = _applicationName
                                  ?? Assembly.GetEntryAssembly()?.GetName().Name
                                  ?? AppDomain.CurrentDomain.FriendlyName;

            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                applicationName = applicationName.Replace(invalid, '_');
            }

            Directory.CreateDirectory(LOG_DIRECTORY);

            var now = DateTime.Now;
            _logFile = Path.Combine(LOG_DIRECTORY, $"{applicationName}_{now:yyyyMMdd_HHmmss_fff}.log");

            try
            {
                var processStart = Process.GetCurrentProcess().StartTime;
                File.AppendAllText(_logFile, $"{processStart.ToString(TIMESTAMP_FORMAT)}\tProcess started\r\n");
            }
            catch (Exception)
            {
                // Not important
            }
        }
    }
}
