using System;
using System.Linq;

using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Invoicing.Core.Data;

namespace Elsa.Invoicing.Generation.Tasks.ContextGenerators
{
    public class ReceivingInvoiceContextGenerator : IContextGenerator
    {
        public const string ReceivingInvoiceGeneratorName = "ReceivingInvoice";

        private readonly IMaterialBatchFacade m_batchFacade;
        
        private readonly IInvoiceFormsRepository m_invoiceFormsRepository;

        public ReceivingInvoiceContextGenerator(IMaterialBatchFacade batchFacade,
            IInvoiceFormsRepository invoiceFormsRepository)
        {
            m_batchFacade = batchFacade;
            m_invoiceFormsRepository = invoiceFormsRepository;
        }

        public bool FillNextContext(GenerationContext context)
        {
            var bridges = m_invoiceFormsRepository.GetInvoiceFormTypeInventories().Where(b =>
                b.InvoiceFormType.GeneratorName.Equals(ReceivingInvoiceGeneratorName,
                    StringComparison.InvariantCultureIgnoreCase)).ToList();

            foreach (var bridge in bridges)
            {
                var remainingBatch =
                    m_batchFacade.FindBatchWithMissingInvoiceItem(bridge.InvoiceFormTypeId, bridge.MaterialInventoryId);
                if (remainingBatch == null)
                {
                    continue;
                }

                context.MaterialInventoryId = bridge.MaterialInventoryId;
                context.InvoiceFormTypeId = bridge.InvoiceFormTypeId;
                context.SourceBatch = remainingBatch;

                return true;
            }

            return false;
        }
    }
}
