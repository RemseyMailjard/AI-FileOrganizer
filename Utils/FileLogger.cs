using System;
using System.IO;

namespace AI_FileOrganizer2.Utils
{
    /// <summary>
    /// Logger die logt naar een tekstbestand. Werkt thread-safe.
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly string _filePath;
        private readonly object _lockObj = new object();

        public FileLogger(string filePath)
        {
            _filePath = filePath;
        }

        public void Log(string message)
        {
            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            lock (_lockObj)
            {
                File.AppendAllText(_filePath, logLine + Environment.NewLine);
            }
        }
    }
}
