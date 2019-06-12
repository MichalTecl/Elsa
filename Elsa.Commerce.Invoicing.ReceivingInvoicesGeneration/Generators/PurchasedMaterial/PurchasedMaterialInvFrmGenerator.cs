using System;
using System.Linq;

using Elsa.Commerce.Core.Warehouse;
using Elsa.Core.Entities.Commerce.Extensions;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Core.Data;
using Elsa.Invoicing.Core.Helpers;

namespace Elsa.Commerce.Invoicing.ReceivingInvoicesGeneration.Generators.PurchasedMaterial
{
    internal class PurchasedMaterialInvFrmGenerator : IInvoiceFormGenerator
    {
        private readonly IMaterialBatchFacade m_batchFacade;
        private readonly BatchesGrouping m_batchesGrouping = new BatchesGrouping();
        private readonly IInvoiceFormsRepository m_invoiceFormsRepository;

        public PurchasedMaterialInvFrmGenerator(IMaterialBatchFacade batchFacade, IInvoiceFormsRepository invoiceFormsRepository)
        {
            m_batchFacade = batchFacade;
            m_invoiceFormsRepository = invoiceFormsRepository;

            #region  Invoice Number
            m_batchesGrouping.AddGrouping((reference, candidate) =>
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

            m_batchesGrouping.AddValidator((b, l) =>
            {
                if (string.IsNullOrWhiteSpace(b.InvoiceNr))
                {
                    l.Warning($"Šarže \"{b.GetTextInfo()}\" nemá číslo faktury");
                }
            });
            #endregion

            #region Invoice variable symbol
            m_batchesGrouping.AddGrouping((reference, candidate) => string.Equals(reference.InvoiceVarSymbol,
                    candidate.InvoiceVarSymbol,
                    StringComparison.InvariantCultureIgnoreCase),
                (reference, candidate, log) =>
                {
                    log.Warning($"Šarže \"{reference.GetTextInfo()}\" a \"{candidate.GetTextInfo()}\" mají stejné číslo faktury, ale rozdínlý variabilní symbol - nemohou být sloučeny do jedné příjemky");
                });

            m_batchesGrouping.AddValidator((b, l) =>
            {
                if (string.IsNullOrWhiteSpace(b.InvoiceVarSymbol))
                {
                    l.Warning($"Šarže \"{b.GetTextInfo()}\" nemá v.s.");
                }
            });
            #endregion

            #region Supplier
            m_batchesGrouping.AddGrouping(
                (reference, candidate) => (reference.SupplierId ?? -1) == (candidate.SupplierId ?? -2),
                (reference, candidate, log) =>
                {
                    if (candidate.SupplierId == null)
                    {
                        log.Warning($"Šarže \"{candidate.GetTextInfo()}\" nemá dodavatele");
                    }
                });

            m_batchesGrouping.AddValidator((b, l) =>
            {
                if (b.SupplierId == null)
                {
                    l.Warning($"Šarže \"{b.GetTextInfo()}\" nemá dodavatele");
                }
            });
            #endregion

            #region Currency
            m_batchesGrouping.AddGrouping((reference, candidate) =>
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
            m_batchesGrouping.AddGrouping((reference, candidate) => reference.Created.Date == candidate.Created.Date, (reference, candidate, log) =>
            {
                log.Warning($"Šarže \"{reference.GetTextInfo()}\" a \"{reference.GetTextInfo()}\" mají datum naskladnění - nebudou na stejné příjemce");
            });
            #endregion

            #region Price
            m_batchesGrouping.AddValidator((b, l) =>
            {
                if (b.Price < 0.00001m)
                {
                    l.Warning($"Šarže \"{b.GetTextInfo()}\" má cenu {b.Price}");
                }
            });
            #endregion

            #region Inventory
            m_batchesGrouping.AddGrouping((reference, candidate) => reference.Material.InventoryId == candidate.Material.InventoryId,
                (reference, candidate, cx) =>
                {
                    cx.Warning($"Šarže \"{reference.GetTextInfo()}\" a \"{candidate.GetTextInfo()}\" nemohou být na jedné příjmece, protože materiály spadají do různých skladů");
                });
            #endregion

        }

        public string GetGenerationName(IMaterialInventory forInventory, int year, int month)
        {
            return $"Příjemky na sklad {forInventory.Name} {month.ToString().PadLeft(2, '0')}/{year}";
        }

        public void Generate(IMaterialInventory forInventory, int year, int month, IInvoiceFormGenerationContext context)
        {
            var formType = m_invoiceFormsRepository.GetInvoiceFormTypes().FirstOrDefault(t => t.GeneratorName == "ReceivingInvoice");
            if (formType == null)
            {
                throw new InvalidOperationException("No InvoiceFormType found by GeneratorName == ReceivingInvoice");
            }

            var startDate = new DateTime(year, month, 1).Date;
            var endDate = startDate.AddMonths(1);
            
            context.Info($"Hledám šarže na skladu {forInventory.Name} od {startDate} do {endDate}");

            var sourceBatches = m_batchFacade.FindBatches(forInventory.Id, startDate, endDate).ToList();

            context.Info($"Nalezeno {sourceBatches.Count}. Začínám indexování");

            var groups = m_batchesGrouping.GroupBatches(sourceBatches, context).ToList();

            context.Info($"Sestaveno {sourceBatches.Count} skupin");

            foreach (var group in groups)
            {
                var referenceBatch = group.FirstOrDefault();
                if (referenceBatch == null)
                {
                    continue;
                }

                var form = context.NewInvoiceForm(f =>
                {
                    f.InvoiceFormNumber = $"NESCHVALENO_{Guid.NewGuid():N}";
                    f.InvoiceNumber = referenceBatch.InvoiceNr;
                    f.InvoiceVarSymbol = referenceBatch.InvoiceVarSymbol;
                    f.IssueDate = referenceBatch.Created.Date;
                    f.MaterialInventoryId = referenceBatch.Material.InventoryId;
                    f.SupplierId = referenceBatch.SupplierId;
                    f.FormTypeId = formType.Id;
                });

                foreach (var batch in group)
                {
                    var existingCollection = m_invoiceFormsRepository.GetCollectionByMaterialBatchId(batch.Id);
                    if (existingCollection != null)
                    {
                        context.Error($"Šarže \"{batch.GetTextInfo()}\" je již zahrnuta v soupisce příjemek \"{existingCollection.Name}\", novou soupisku není možné vygenerovat");
                    }

                    m_invoiceFormsRepository.NewItem(form,
                        batch.Id,
                        item =>
                        {
                            item.MaterialName = batch.Material.Name;
                            item.Quantity = batch.Volume;
                            item.UnitId = batch.UnitId;

                            var price = m_batchFacade.CalculateBatchPrice(batch.Id);

                            item.PrimaryCurrencyPrice = price.PrimaryCurrencyPrice;
                            if (price.Conversion != null)
                            {
                                item.SourceCurrencyPrice = price.Conversion.SourceValue;
                                item.SourceCurrencyId = price.Conversion.SourceCurrencyId;
                                item.ConversionId = price.Conversion.Id;
                            }
                        });
                }
            }

        }

       
    }
}
