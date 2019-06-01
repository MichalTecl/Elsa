using System;
using System.Linq;

using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common.Logging;
using Elsa.Invoicing.Core.Data;

namespace Elsa.Invoicing.Generation.Tasks.ContextGenerators
{
    public class ReceivingInvoiceContextGenerator : IContextGenerator
    {
        public const string ReceivingInvoiceGeneratorName = "ReceivingInvoice";

        private readonly IMaterialBatchFacade m_batchFacade;
        private readonly IInvoiceFormsRepository m_invoiceFormsRepository;
        private readonly ILog m_log;

        public ReceivingInvoiceContextGenerator(IMaterialBatchFacade batchFacade,
            IInvoiceFormsRepository invoiceFormsRepository,
            ILog log)
        {
            m_batchFacade = batchFacade;
            m_invoiceFormsRepository = invoiceFormsRepository;
            m_log = log;
        }

        public bool FillNextContext(GenerationContext context)
        {
            m_log.Info("Zacinam nastavovat kontext pro nove generovani prijemek");

            var invoiceFormType = m_invoiceFormsRepository.GetInvoiceFormTypes()
                .FirstOrDefault(t => t.GeneratorName == ReceivingInvoiceGeneratorName);

            if (invoiceFormType == null)
            {
                throw new InvalidOperationException($"No InvoiceForType found by GeneratorName = '{ReceivingInvoiceGeneratorName}'");
            }

            var remainingBatch = m_batchFacade.FindBatchWithMissingInvoiceItem(invoiceFormType.Id);
            if (remainingBatch == null)
            {
                m_log.Info("zadne dalsi sarze pro generovani prijemek");
                return false;
            }
            
            context.InvoiceFormTypeId = invoiceFormType.Id;
            context.SourceBatch = remainingBatch;

            m_log.Info($"Vytvoren kontext pro generovani prijemky pro sarzi {remainingBatch.BatchNumber}");

            return true;
        }
    }
}
