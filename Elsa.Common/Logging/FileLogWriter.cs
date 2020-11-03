using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Common.Logging;

namespace Elsa.Common.Logging
{
    public class FileLogWriter : ILogWriter
    {
        private readonly object m_queueLock = new object();

        private readonly string m_type;

        public FileLogWriter(string type)
        {
            m_type = type;
        }

        public void Write(ISysLog entry)
        {
            var severity = entry.IsError ? "ERR" : "INF";
            var timeInfo = entry.IsStopWatch ? $"\tTIME_MS:{entry.MeasuredTime}" : string.Empty;
            var sEntry = $"{(char)1}{entry.EventDt: HH:mm:ss}\t{severity}\t{entry.SessionId}\t{entry.Method}\t{entry.Message}\t{timeInfo}";

            lock (m_queueLock)
            {
                using (var strm = OpenFile())
                using (var writer = new StreamWriter(strm, Encoding.UTF8))
                {
                    writer.WriteLine(sEntry);
                }
            }
        }
        private Stream OpenFile()
        {
            var directory = $"C:\\Elsa\\Log\\{m_type}";

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var file = $"{directory}\\{DateTime.Now:yyyyMMdd}.log";

            return new FileStream(file, FileMode.Append);
        }
    }
}
