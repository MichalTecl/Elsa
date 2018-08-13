using System;

namespace Elsa.Common.Logging
{
    public static class LogFactory
    {
        public static ILog Get()
        {
            return new EmptyLog();
        }

        private class EmptyLog : ILog
        {
            public void Debug(string s)
            {
            }

            public void Error(string s, Exception e)
            {
            }

            public void Error(string s)
            {
            }
        }
    }
}
