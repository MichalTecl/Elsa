using System;
using System.Runtime.CompilerServices;

using Elsa.Core.Entities.Commerce.Common.Logging;

using Robowire.RobOrm.Core;

namespace Elsa.Common.Logging
{
    public class Logger : ILog
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;

        public Logger(IDatabase database, ISession session)
        {
            m_database = database;
            m_session = session;
        }

        public void Info(string s, 
            [CallerMemberName] string member = "",
            [CallerFilePath] string path = "",
            [CallerLineNumber] int line = 0)
        {
            CreateEntry(member, path, line, e => e.Message = s);
            Console.WriteLine(s);
        }

        public void Error(string s, Exception e,
            [CallerMemberName] string member = "",
            [CallerFilePath] string path = "",
            [CallerLineNumber] int line = 0)
        {
            var spacer = e == null ? string.Empty : "\t";

            var msg = $"{s}{spacer}{e?.Message ?? string.Empty} {e?.ToString() ?? string.Empty}";
            CreateEntry(member, path, line,
                entry =>
                    {
                        entry.IsError = true;
                        entry.Message = msg;
                    });

            Console.WriteLine(s);
            Console.WriteLine(e);
        }

        public void Error(string s,
            [CallerMemberName] string member = "",
            [CallerFilePath] string path = "",
            [CallerLineNumber] int line = 0)
        {
            Error(s, null, member, path, line);
        }

        public IDisposable StartStopwatch(string actionName)
        {
            throw new NotImplementedException();
        }

        private void CreateEntry(string member, string path, int line, Action<ISysLog> entrySetter)
        {
            var lastBackSlash = path?.LastIndexOf("\\", StringComparison.Ordinal) ?? -1;
            if (lastBackSlash > -1)
            {
                path = path.Substring(lastBackSlash + 1);

                if (path.EndsWith(".cs"))
                {
                    path = path.Substring(0, path.Length - 3); 
                }
            }
            

            var entry = m_database.New<ISysLog>();

            entry.EventDt = DateTime.Now;
            entry.SessionId = m_session.SessionId;
            entry.Method = $"{path}.{member}:{line}";

            entrySetter(entry);

            AsyncLogger.Write(entry);
        }
    }
}
