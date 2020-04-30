using System;
using System.Collections.Generic;
using System.Text;

namespace Schedoo.Core
{
    internal sealed class JobContextImpl : IJobContext
    {
        private readonly IDataRepository m_data;
        private readonly IJob m_job;
        private readonly JobStatus m_status;
        private readonly StringBuilder m_log = new StringBuilder();

        public JobContextImpl(IDataRepository data, IJob job)
        {
            m_data = data;
            m_job = job;
            
            m_status = new JobStatus(data.GetLastJobStarted(job), m_data.GetLastJobSucceeded(job), m_data.GetLastJobFailed(job));

            m_log.Append($"Created context for {job.Uid}: ");
        }

        public bool NowIsBetween(int minHour, int maxHour)
        {
            m_log.Append($"cond.NowIsBetween({minHour}, {maxHour})=");

            if (maxHour < minHour)
            {
                return !NowIsBetween(maxHour, minHour);
            }

            var res = (m_data.Now.Hour >= minHour) && (m_data.Now.Hour <= maxHour);

            m_log.Append($"{res} ");

            return res;
        }

        public bool DidntRunMoreThan(int hours, int minutes, int seconds)
        {
            m_log.Append($"cond.DidntRunMoreThan({hours}:{minutes}:{seconds})=");

            if (m_status.IsNowRunning)
            {
                m_log.Append("false (is running now) ");
                return false;
            }

            var lastEnd = m_data.Now - m_status.LastEndDateTime;

            var res = lastEnd >= new TimeSpan(0, hours, minutes, seconds);
            m_log.Append($"{res} (last={lastEnd}) ");

            return res;
        }

        public bool LastTimeFailed()
        {
            var res =  m_status.CurrentStatusIsSucceeded == false;
            m_log.Append($"cond.LastTimeFailed={res} ");
            return res;
        }

        public bool LastTimeSucceded()
        {
            var res = m_status.CurrentStatusIsSucceeded == true;
            m_log.Append($"cond.LastTimeSucceeded={res} ");
            return res;
        }

        public bool IsNowRunning()
        {
            var res = m_status.IsNowRunning;
            m_log.Append($"cond.IsNowRunning={res} ");
            return res;
        }

        public override string ToString()
        {
            return m_log.ToString();
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
                        return DateTime.MinValue;
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
