using System;
using System.Runtime.CompilerServices;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Common.Logging;

using Robowire.RobOrm.Core;

namespace Elsa.Common.Logging
{
    public class Logger : ILog
    {
        private readonly ISession _session;
        private readonly ILogWriter _logWriter;

        public Logger(ISession session, ILogWriter logWriter)
        {
            _session = session;
            _logWriter = logWriter;
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
            for (var current = e; current != null; current = current.InnerException)
            {
                var diagnosticId = current.Data[Robowire.RobOrm.SqlServer.DeadlockDiagnostics.EXCEPTION_DATA_KEY];
                if (diagnosticId == null)
                    continue;

                s = $"DeadlockDiagnosticId={diagnosticId}; {s}";
                break;
            }

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

            _logWriter.Write(entry);
        }

        private ISysLog CreateEntry(string member, string path, int line)
        {
            var entry = new Entry
            {
                EventDt = DateTime.Now,
                SessionId = _session.SessionId,
                Method = $"{path}.{member}:{line}"
            };


            return entry;
        }
        
        protected virtual void OnBeforeEntryEnqueue(ISysLog entry) { }

        private sealed class StopWatch : IDisposable
        {
            private readonly Logger _owner;
            private readonly DateTime _startTime;
            private readonly string _watchName;
            private readonly string _member;
            private readonly string _path;
            private readonly int _line;

            public StopWatch(Logger owner, string watchName, string member, string path, int line)
            {
                _owner = owner;
                _watchName = watchName;
                _member = member;
                _path = path;
                _line = line;
                _startTime = DateTime.Now;
            }

            public void Dispose()
            {
                var time = (DateTime.Now - _startTime).TotalMilliseconds;

                _owner.CreateEntry(
                    _member,
                    _path,
                    _line,
                    e =>
                        {
                            e.MeasuredTime = (int)time;
                            e.IsStopWatch = true;
                            e.Message = _watchName;
                        });
            }
        }

        private class Entry : ISysLog
        {
            public long Id { get; }

            public long? SessionId { get; set; }

            public DateTime EventDt { get; set; }

            public bool IsError { get; set; }

            public bool IsStopWatch { get; set; }

            public int? MeasuredTime { get; set; }

            public string Method { get; set; }

            public string Message { get; set; }
        }
    }
}
