using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Extensions;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Core.Data;
using Elsa.Invoicing.Core.Helpers;

namespace Elsa.Commerce.Invoicing.ReceivingInvoicesGeneration.Generators.PremanufacturedMixtures
{
    internal class PremanufacturedMixturesInvFrmGenerator:ReceivingInvoiceGeneratorBase
    {
        private readonly IMaterialRepository m_materialRepository;

        public PremanufacturedMixturesInvFrmGenerator(IMaterialBatchFacade batchFacade,
            IInvoiceFormsRepository invoiceFormsRepository,
            IMaterialRepository materialRepository)
            : base(batchFacade, invoiceFormsRepository)
        {
            m_materialRepository = materialRepository;
        }

        protected override void SetupGrouping(BatchesGrouping grouping)
        {
            grouping.AddGrouping((c,n) => false, null);

            #region Production price
            grouping.AddValidator((b, c) =>
            {
                if (b.ProductionWorkPrice == null && b.Price == 0)
                {
                    c.Warning($"Šarže \"{b.GetTextInfo()}\" nemá uvedenu cenu práce při výrobě");
                }
            });
            #endregion

            #region Foregin currency
            grouping.AddValidator((b, c) =>
            {
                if (b.PriceConversion != null)
                {
                    c.Warning($"Šarže má \"{b.GetTextInfo()}\" cenu zkonvertovanou z cizí měny");
                }
            });
            #endregion
        }

        protected override void CustomizeItemMapping(IInvoiceForm form,
            IInvoiceFormItem item,
            IMaterialBatch batch,
            IInvoiceFormGenerationContext context)
        {
        }

        protected override void CustomizeFormMapping(IMaterialBatch referenceBatch, IInvoiceForm form, IInvoiceFormGenerationContext context)
        {
        }

        protected override IEnumerable<IMaterialBatch> FindSourceBatches(IMaterialInventory forInventory,
            int year,
            int month,
            IMaterialBatchFacade facade,
            IInvoiceFormGenerationContext context)
        {
            var to = new DateTime(year, month, 1).AddMonths(1);

            var batches = facade.FindNotClosedBatches(forInventory.Id,
                new DateTime(year, month, 1).AddYears(-1),
                to,
                b =>
                {
                    var acod = facade.GetBatchAccountingDate(b);

                    if ((acod.AccountingDate.Month != month) || (acod.AccountingDate.Year != year))
                    {
                        return false;
                    }

                    if (!acod.IsFinal)
                    {
                        if ((acod.AccountingDate.Year == year) && (acod.AccountingDate.Month == month))
                        context.Warning($"Šarže {b.GetTextInfo()} má účetní datum {acod} ale datum nelze stanovit jako konečné: \"{acod.NotFinalReason}\". Příjemka nebude vytvořena.");

                        return false;
                    }

                    return true;
                });

            return batches;
        }
    }
}
