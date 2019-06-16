using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Warehouse;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Extensions;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Core.Data;
using Elsa.Invoicing.Core.Helpers;

namespace Elsa.Commerce.Invoicing.ReceivingInvoicesGeneration.Generators.PurchasedMaterial
{
    internal class PurchasedMaterialInvFrmGenerator : ReceivingInvoiceGeneratorBase
    {
        public PurchasedMaterialInvFrmGenerator(IMaterialBatchFacade batchFacade, IInvoiceFormsRepository invoiceFormsRepository)
            : base(batchFacade, invoiceFormsRepository)
        {
        }

        protected override void SetupGrouping(BatchesGrouping grouping)
        {
            #region  Invoice Number
            grouping.AddGrouping((reference, candidate) =>
            {
                if (string.IsNullOrWhiteSpace(reference.InvoiceNr) ||
                    string.IsNullOrWhiteSpace(candidate.InvoiceNr))
                {
                    return false;
                }

                return string.Equals(reference.InvoiceNr,
                    candidate.InvoiceNr,
                    StringComparison.InvariantCultureIgnoreCase);
            }, null);

            grouping.AddValidator((b, l) =>
            {
                if (string.IsNullOrWhiteSpace(b.InvoiceNr))
                {
                    l.Warning($"Šarže \"{b.GetTextInfo()}\" nemá číslo faktury");
                }
            });
            #endregion

            #region Invoice variable symbol
            grouping.AddGrouping((reference, candidate) => string.Equals(reference.InvoiceVarSymbol,
                    candidate.InvoiceVarSymbol,
                    StringComparison.InvariantCultureIgnoreCase),
                (reference, candidate, log) =>
                {
                    log.Warning($"Šarže \"{reference.GetTextInfo()}\" a \"{candidate.GetTextInfo()}\" mají stejné číslo faktury, ale rozdílný variabilní symbol - nemohou být sloučeny do jedné příjemky");
                });

            grouping.AddValidator((b, l) =>
            {
                if (string.IsNullOrWhiteSpace(b.InvoiceVarSymbol))
                {
                    l.Warning($"Šarže \"{b.GetTextInfo()}\" nemá v.s.");
                }
            });
            #endregion

            #region Supplier
            grouping.AddGrouping(
                (reference, candidate) => (reference.SupplierId ?? -1) == (candidate.SupplierId ?? -2),
                (reference, candidate, log) =>
                {
                    if (candidate.SupplierId == null)
                    {
                        log.Warning($"Šarže \"{candidate.GetTextInfo()}\" nemá dodavatele");
                    }
                });

            grouping.AddValidator((b, l) =>
            {
                if (b.SupplierId == null)
                {
                    l.Warning($"Šarže \"{b.GetTextInfo()}\" nemá dodavatele");
                }
            });
            #endregion

            #region Currency
            grouping.AddGrouping((reference, candidate) =>
            {
                var refRateId = reference.PriceConversion?.CurrencyRateId ?? -1;
                var canRateId = candidate.PriceConversion?.CurrencyRateId ?? -1;

                return refRateId == canRateId;
            },
                (reference, candidate, log) =>
                {
                    log.Warning($"Šarže \"{reference.GetTextInfo()}\" a \"{reference.GetTextInfo()}\" mají rozdílné cizí měny, nebo použitý převodní kurz - nebudou na stejné příjemce");
                });
            #endregion

            #region Purchase Date
            grouping.AddGrouping((reference, candidate) => reference.Created.Date == candidate.Created.Date, (reference, candidate, log) =>
            {
                log.Warning($"Šarže \"{reference.GetTextInfo()}\" a \"{reference.GetTextInfo()}\" mají datum naskladnění - nebudou na stejné příjemce");
            });
            #endregion

            #region Price
            grouping.AddValidator((b, l) =>
            {
                if (b.Price < 0.00001m)
                {
                    l.Warning($"Šarže \"{b.GetTextInfo()}\" má cenu {b.Price}");
                }
            });
            #endregion

            #region Inventory
            grouping.AddGrouping((reference, candidate) => reference.Material.InventoryId == candidate.Material.InventoryId,
                (reference, candidate, cx) =>
                {
                    cx.Warning($"Šarže \"{reference.GetTextInfo()}\" a \"{candidate.GetTextInfo()}\" nemohou být na jedné příjmece, protože materiály spadají do různých skladů");
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

        protected override IEnumerable<IMaterialBatch> FindSourceBatches(IMaterialInventory forInventory, int year, int month, IMaterialBatchFacade facade, IInvoiceFormGenerationContext context)
        {
            var startDate = new DateTime(year, month, 1).Date;
            var endDate = startDate.AddMonths(1);

            context.Info($"Hledám šarže na skladu {forInventory.Name} od {startDate} do {endDate}");

            return facade.FindNotClosedBatches(forInventory.Id, startDate, endDate).ToList();
        }
    }
}
