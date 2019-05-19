using System;
using System.Collections.Generic;

using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Generation.Tasks.ContextGenerators;
using Elsa.Invoicing.Generation.Tasks.ReceivingInvoicesTasks;

using Robowire;

namespace Elsa.Invoicing.Generation
{
    internal class JobsSetup
    {
        public IList<IInvoiceFormGenerationJob> CreateJobs(IServiceLocator locator)
        {
            var result = new List<IInvoiceFormGenerationJob>();

            // Receiving invoice job
            result.Add(new GenerationRunner(locator,
                "ReceivingInvoice",
                typeof(ReceivingInvoiceContextGenerator),
                new List<Type>()
                {
                    typeof(FindInvoiceTask),
                    typeof(CreateInvoiceTask),
                    typeof(PopulateInvoiceTask),
                    typeof(PopulateInvoiceItemTask),
                    typeof(SaveInvoiceTask)
                }));

            return result;
        }
    }
}
