using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.CurrencyRates;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Invoicing.Core.Data;

namespace Elsa.Invoicing.Generation.Tasks.ReceivingInvoicesTasks
{
    public class PopulateInvoiceItemTask : IGenerationTask
    {
        private readonly IMaterialRepository m_materialRepository;
        private readonly ICurrencyConversionHelper m_currencyConversionHelper;
        private readonly IInvoiceFormsRepository m_invoiceFormsRepository;

        public PopulateInvoiceItemTask(IMaterialRepository materialRepository, ICurrencyConversionHelper currencyConversionHelper, IInvoiceFormsRepository invoiceFormsRepository)
        {
            m_materialRepository = materialRepository;
            m_currencyConversionHelper = currencyConversionHelper;
            m_invoiceFormsRepository = invoiceFormsRepository;
        }

        public void Run(GenerationContext context)
        {
            var batch = context.SourceBatch;
            if (batch == null)
            {
                throw new InvalidOperationException("SourceBatch expected in context");
            }

            var item = context.GetOrAddItem(m_invoiceFormsRepository, batch.Id);

            var material = m_materialRepository.GetMaterialById(batch.MaterialId);
            if (material == null)
            {
                throw new InvalidOperationException("Batch material not found");
            }

            item.MaterialName = material.Name;
            item.PrimaryCurrencyPrice = batch.Price;
            item.UnitId = batch.UnitId;
            item.Quantity = batch.Volume;

            if (batch.PriceConversionId != null)
            {
                var conversion = m_currencyConversionHelper.GetConversion(batch.PriceConversionId.Value);
                item.SourceCurrencyId = conversion.SourceCurrencyId;
                item.SourceCurrencyPrice = conversion.SourceValue;
                item.ConversionId = conversion.Id;
            }
        }
    }
}
