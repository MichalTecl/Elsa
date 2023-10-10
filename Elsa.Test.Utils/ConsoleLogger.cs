using Elsa.Common.Logging;
using System;
using System.Runtime.CompilerServices;

namespace Elsa.Test.Utils
{
    public class ConsoleLogger : ILog, IDisposable
    {
        public static readonly ILog Instance = new ConsoleLogger();

        private ConsoleLogger() { }

        public void Dispose()
        {
            Console.WriteLine("Stopwatch doisposed");
        }

        public void Error(string s, Exception e, [CallerMemberName] string member = "", [CallerFilePath] string path = "", [CallerLineNumber] int line = 0)
        {
            Console.WriteLine(s);
        }

        public void Error(string s, [CallerMemberName] string member = "", [CallerFilePath] string path = "", [CallerLineNumber] int line = 0)
        {
            Console.WriteLine(s);
        }

        public void Info(string s, [CallerMemberName] string member = "", [CallerFilePath] string path = "", [CallerLineNumber] int line = 0)
        {
            Console.WriteLine(s);
        }

        public IDisposable StartStopwatch(string actionName, [CallerMemberName] string member = "", [CallerFilePath] string path = "", [CallerLineNumber] int line = 0)
        {
            return this;
        }
    }
}
