using System;

using Schedoo.Core;

namespace Elsa.JobLauncher.Scheduler
{
    public class ElsaJob : IJob
    {
        private readonly Func<IJobContext, bool> m_eval;

        public ElsaJob(string uid, int priority, TimeSpan runTimeout, Func<IJobContext, bool> eval)
        {
            Uid = uid;
            Priority = priority;
            RunTimeout = runTimeout;

            m_eval = eval;
        }

        public int Priority { get; }

        public string Uid { get; }

        public TimeSpan RunTimeout { get; }

        public bool EvalPreconditions(IJobContext c)
        {
            return m_eval(c);
        }

        public override string ToString()
        {
            return Uid;
        }
    }
}
