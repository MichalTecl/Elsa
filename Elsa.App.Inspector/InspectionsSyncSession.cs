using System;
using System.Threading;

namespace Elsa.App.Inspector
{
    public class InspectionsSyncSession : IDisposable
    {
        private readonly Mutex m_mutex = new Mutex(false, "Global\\ElsaInspectionsMutex");
        private static readonly object s_lock = new object();

        public int SessionId { get; set; }

        public bool IsAcquired { get; private set; }

        public InspectionsSyncSession()
        {
            lock (s_lock)
            {
                try
                {
                    if (!m_mutex.WaitOne(TimeSpan.FromSeconds(5)))
                    {
                        throw new InvalidOperationException("Inspektor je právě zaneprázdněn, zkuste to později");
                    }
                }
                catch (AbandonedMutexException e)
                {
                }

                IsAcquired = true;
            }
        }

        public void Sync(Action action)
        {
            lock (s_lock)
            {
                if (!IsAcquired)
                {
                    throw new InvalidOperationException("Invalid session state");
                }

                action();
            }
        }


        public void Dispose()
        {
            lock (s_lock)
            {
                if (IsAcquired)
                {
                    m_mutex.ReleaseMutex();
                }

                m_mutex?.Dispose();

                IsAcquired = false;
            }
        }
    }
}
