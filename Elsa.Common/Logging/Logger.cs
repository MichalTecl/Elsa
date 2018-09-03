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

        public IDisposable StartStopwatch(string actionName,
            [CallerMemberName] string member = "",
            [CallerFilePath] string path = "",
            [CallerLineNumber] int line = 0)
        {
            return new StopWatch(this, actionName, member, path, line);
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

            var entry = CreateEntry(member, path, line);

            entrySetter(entry);

            if (entry.Message.Length > 1000)
            {
                entry.Message = $"{entry.Message.Substring(0, 997)}...";
            }

            AsyncLogger.Write(entry);
        }

        private ISysLog CreateEntry(string member, string path, int line)
        {
            var entry = m_database.New<ISysLog>();

            entry.EventDt = DateTime.Now;
            entry.SessionId = m_session.SessionId;
            entry.Method = $"{path}.{member}:{line}";

            return entry;
        }

        private sealed class StopWatch : IDisposable
        {
            private readonly Logger m_owner;
            private readonly DateTime m_startTime;
            private readonly string m_watchName;
            private readonly string m_member;
            private readonly string m_path;
            private readonly int m_line;

            public StopWatch(Logger owner, string watchName, string member, string path, int line)
            {
                m_owner = owner;
                m_watchName = watchName;
                m_member = member;
                m_path = path;
                m_line = line;
                m_startTime = DateTime.Now;
            }

            public void Dispose()
            {
                var time = (DateTime.Now - m_startTime).TotalMilliseconds;

                m_owner.CreateEntry(
                    m_member,
                    m_path,
                    m_line,
                    e =>
                        {
                            e.MeasuredTime = (int)time;
                            e.IsStopWatch = true;
                            e.Message = m_watchName;
                        });
            }
        }

    }
}
