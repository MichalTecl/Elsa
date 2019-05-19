using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Invoicing.Core.Contract;

using Robowire;

namespace Elsa.Invoicing.Core
{
    public class CommonInvoiceFormGenerationJob
    {
        private readonly IServiceLocator m_locator;

        public CommonInvoiceFormGenerationJob(IServiceLocator locator)
        {
            m_locator = locator;
        }

        public void Start()
        {
            var jobs = m_locator.GetCollection<IInvoiceFormGenerationJob>();

            foreach (var job in jobs)
            {
                job.Start();
            }
        }
    }
}
