using System;

namespace Schedoo.Core
{
    internal sealed class JobContextImpl : IJobContext
    {
        private readonly IDataRepository m_data;
        private readonly IJob m_job;
        private readonly JobStatus m_status;

        public JobContextImpl(IDataRepository data, IJob job)
        {
            m_data = data;
            m_job = job;

            m_status = new JobStatus(data.GetLastJobStarted(job), m_data.GetLastJobSucceeded(job), m_data.GetLastJobFailed(job));
        }

        public bool NowIsBetween(int minHour, int maxHour)
        {
            if (maxHour < minHour)
            {
                return !NowIsBetween(maxHour, minHour);
            }

            return (m_data.Now.Hour >= minHour) && (m_data.Now.Hour <= maxHour);
        }

        public bool DidntRunMoreThan(int hours, int minutes, int seconds)
        {
            if (m_status.IsNowRunning)
            {
                return false;
            }

            var lastEnd = m_data.Now - m_status.LastEndDateTime;

            return lastEnd >= new TimeSpan(0, hours, minutes, seconds);
        }

        public bool LastTimeFailed()
        {
            return m_status.CurrentStatusIsSucceeded == false;
        }

        public bool LastTimeSucceded()
        {
            return m_status.CurrentStatusIsSucceeded == true;
        }

        public bool IsNowRunning()
        {
            return m_status.IsNowRunning;
        }

        private class JobStatus
        {
            private readonly DateTime? m_lastStart;
            private readonly DateTime? m_lastSucc;
            private readonly DateTime? m_lastFail;

            public JobStatus(DateTime? lastStart, DateTime? lastSucc, DateTime? lastFail)
            {
                m_lastStart = lastStart;
                m_lastSucc = lastSucc;
                m_lastFail = lastFail;
            }

            public bool? CurrentStatusIsSucceeded
            {
                get
                {
                    if ((m_lastStart == null) || ((m_lastSucc == null) && (m_lastFail == null)))
                    {
                        return null;
                    }

                    var sd = m_lastStart ?? DateTime.MaxValue;
                    var suc = m_lastSucc ?? DateTime.MinValue;
                    var err = m_lastFail ?? DateTime.MinValue;

                    if ((sd > suc) && (sd > err))
                    {
                        return null;
                    }

                    return (sd < suc) && (suc > err);
                }
            }

            public bool IsNowRunning
            {
                get
                {
                    if (m_lastStart == null)
                    {
                        return false;
                    }

                    return (CurrentStatusIsSucceeded == null);
                }
            }

            public DateTime LastEndDateTime
            {
                get
                {
                    if (m_lastStart == null)
                    {
                        return DateTime.MaxValue;
                    }

                    if (IsNowRunning)
                    {
                        return DateTime.MaxValue;
                    }

                    var suc = m_lastSucc ?? DateTime.MinValue;
                    var err = m_lastFail ?? DateTime.MinValue;

                    if (suc > err)
                    {
                        return suc;
                    }

                    return err;
                }
            }
            
        }
    }
}
