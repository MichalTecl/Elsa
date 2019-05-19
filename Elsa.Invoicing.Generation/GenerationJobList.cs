using System;
using System.Collections;
using System.Collections.Generic;

using Elsa.Invoicing.Core.Contract;

namespace Elsa.Invoicing.Generation
{
    public class GenerationJobList : IEnumerable<IInvoiceFormGenerationJob>
    {
        private readonly List<IInvoiceFormGenerationJob> m_jobs;

        public GenerationJobList(IEnumerable<IInvoiceFormGenerationJob> jobs)
        {
            m_jobs = new List<IInvoiceFormGenerationJob>(jobs);
        }

        public IEnumerator<IInvoiceFormGenerationJob> GetEnumerator()
        {
            return m_jobs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
