using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Schedoo.Core
{
    public abstract class SchedulerBase
    {
        private readonly IDataRepository m_dataRepository;

        private readonly TimeSpan m_tick;
        
        protected SchedulerBase(IDataRepository dataRepository, TimeSpan tick)
        {
            m_dataRepository = dataRepository;
            m_tick = tick;
        }

        public void Start(string forcedJobName = null)
        {
            //while (true)
            //{
                try
                {
                    var jobs = m_dataRepository.AllJobs.Where(j => string.IsNullOrWhiteSpace(forcedJobName) || j.Uid == forcedJobName).OrderBy(j => j.Priority).ToList();

                    if (!string.IsNullOrWhiteSpace(forcedJobName) && jobs.Count != 1)
                    {
                        throw new InvalidOperationException($"{jobs.Count} found by \"{forcedJobName}\"");
                    }

                    foreach (var job in jobs)
                    {
                        var context = CreateContext(job);
                        
                        try
                        {
                            if (context.IsNowRunning())
                            {
                                var lastStart = m_dataRepository.GetLastJobStarted(job) ?? DateTime.MinValue;
                                if ((m_dataRepository.Now - lastStart) > job.RunTimeout)
                                {
                                    throw new TimeoutException("Job timed out");
                                }

                                continue;
                            }

                            var preconditons = (job.Uid == forcedJobName) || EvalPreconditions(job, context);
                            WriteContextLog(context, preconditons);
                            if (!preconditons)
                            {
                                continue;
                            }

                            OnJobStart(job);

                            m_dataRepository.OnJobStart(job);
                            StartJob(job);
                            m_dataRepository.OnJobSucceeded(job);

                            OnJobSucceeded(job);
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                m_dataRepository.OnJobFailed(job, ex);
                            }
                            catch
                            {
                                ;
                            }

                            try
                            {
                                OnJobFailed(job, ex);
                            }
                            catch
                            {
                                ;
                            }
                        }
                    }

                    Thread.Sleep(m_tick);
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine("GENERAL JOB LAUNCHER ERROR:");
                    Console.WriteLine(ex);

                    try
                    {
                        File.WriteAllText($"error_{Guid.NewGuid()}.txt", ex.ToString());
                }
                    catch
                    {
                        ;
                    }

                    Thread.Sleep(5000);
                }
            //}
        }

        protected abstract void StartJob(IJob job);

        protected virtual bool EvalPreconditions(IJob job, IJobContext context)
        {
            try
            {
                return job.EvalPreconditions(context);
            }
            catch (Exception ex)
            {
                OnPreconditionEvaluationFailed(job, ex);
                return false;
            }
        }

        protected virtual void OnPreconditionEvaluationFailed(IJob job, Exception ex)
        {
            throw ex;
        }

        protected virtual void OnContextCreationFailed(IJob job, Exception ex)
        {
        }

        protected virtual IJobContext CreateContext(IJob job)
        {
            try
            {
                return new JobContextImpl(m_dataRepository, job);
            }
            catch (Exception ex)
            {
                OnContextCreationFailed(job, ex);
                throw;
            }
        }

        protected virtual void OnJobFailed(IJob job, Exception ex)
        {
        }

        protected virtual void OnJobStart(IJob job)
        {
        }

        protected virtual void OnJobSucceeded(IJob job)
        {
        }

        protected abstract void WriteContextLog(IJobContext context, bool preconditonsEvaluationResult);
    }
}
