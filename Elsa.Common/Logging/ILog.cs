using System;
using System.Runtime.CompilerServices;

namespace Elsa.Common.Logging
{
    public interface ILog
    {
        void Info(string s,
            [CallerMemberName] string member = "",
            [CallerFilePath] string path = "",
            [CallerLineNumber] int line = 0);

        void Error(string s, Exception e,
            [CallerMemberName] string member = "",
            [CallerFilePath] string path = "",
            [CallerLineNumber] int line = 0);

        void Error(string s,
            [CallerMemberName] string member = "",
            [CallerFilePath] string path = "",
            [CallerLineNumber] int line = 0);

        IDisposable StartStopwatch(string actionName,
            [CallerMemberName] string member = "",
            [CallerFilePath] string path = "",
            [CallerLineNumber] int line = 0);
    }
}
