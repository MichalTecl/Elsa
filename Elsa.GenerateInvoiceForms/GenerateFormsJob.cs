using System;
using System.Collections.Generic;

using Elsa.Common.Logging;
using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Generation;
using Elsa.Jobs.Common;

using Robowire;

namespace Elsa.GenerateInvoiceForms
{
    public class GenerateFormsJob : IExecutableJob
    {
        private readonly IServiceLocator m_serviceLocator;
        private readonly ILog m_log;

        public GenerateFormsJob(IServiceLocator serviceLocator, ILog log)
        {
            m_serviceLocator = serviceLocator;
            m_log = log;
        }

        public void Run(string customDataJson)
        {
            var jobList = m_serviceLocator.Get<GenerationJobList>();

            foreach (var job in jobList)
            {
                try
                {
                    m_log.Info($"Starting {job.Name}");

                    job.Start();

                    m_log.Info($"Finished {job.Name}");
                }
                catch (Exception ex)
                {
                    m_log.Error($"FAILED {job.Name}", ex);
                }
            }
        }
    }
}
