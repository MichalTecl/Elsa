using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Commerce.Core.Model.BatchPriceExpl;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Extensions;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Core.Data;
using Elsa.Invoicing.Core.Helpers;

namespace Elsa.Commerce.Invoicing.ReceivingInvoicesGeneration.Generators
{
    internal abstract class ReceivingInvoiceGeneratorBase : IInvoiceFormGenerator
    {
        private readonly IMaterialBatchFacade m_batchFacade;
        private readonly BatchesGrouping m_batchesGrouping = new BatchesGrouping();
        private readonly IInvoiceFormsRepository m_invoiceFormsRepository;

        private bool m_groupingSetup;

        protected ReceivingInvoiceGeneratorBase(IMaterialBatchFacade batchFacade, IInvoiceFormsRepository invoiceFormsRepository)
        {
            m_batchFacade = batchFacade;
            m_invoiceFormsRepository = invoiceFormsRepository;
        }

        protected abstract void SetupGrouping(BatchesGrouping grouping);

        public string GetGenerationName(IMaterialInventory forInventory, int year, int month)
        {
            return $"Příjemky na sklad {forInventory.Name} {month.ToString().PadLeft(2, '0')}/{year}";
        }

        public void Generate(IMaterialInventory forInventory, int year, int month, IInvoiceFormGenerationContext context, IReleasingFormsGenerationTask task = null)
        {
            if (task != null)
            {
                throw new InvalidOperationException("Illegal usage of generator");
            }

            if (!m_groupingSetup)
            {
                SetupGrouping(m_batchesGrouping);
                m_groupingSetup = true;
            }

            var formType = m_invoiceFormsRepository.GetInvoiceFormTypes().FirstOrDefault(t => t.GeneratorName == "ReceivingInvoice");
            if (formType == null)
            {
                throw new InvalidOperationException("No InvoiceFormType found by GeneratorName == ReceivingInvoice");
            }

            var sourceBatches = FindSourceBatches(forInventory, year, month, m_batchFacade, context).Where(b => b.IsHiddenForAccounting != true).ToList();

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

                var priceIndex = group.ToDictionary(b => b.Id, b => m_batchFacade.GetBatchPrice(b.Id));

                var totalPrice = BatchPrice.Combine(priceIndex.Values);

                var form = context.NewInvoiceForm(f =>
                {
                    f.InvoiceFormNumber = $"NESCHVALENO_{Guid.NewGuid():N}";
                    f.InvoiceNumber = referenceBatch.InvoiceNr;
                    f.InvoiceVarSymbol = referenceBatch.InvoiceVarSymbol;
                    f.IssueDate = m_batchFacade.GetBatchAccountingDate(referenceBatch).AccountingDate;
                    f.MaterialInventoryId = referenceBatch.Material.InventoryId;
                    f.SupplierId = referenceBatch.SupplierId;
                    f.FormTypeId = formType.Id;
                    f.PriceCalculationLog = totalPrice.Text;
                    f.PriceHasWarning = totalPrice.HasWarning;
                });

                CustomizeFormMapping(referenceBatch, form, context);
                
                foreach (var batch in group)
                {
                    var existingCollection = m_invoiceFormsRepository.GetCollectionByMaterialBatchId(batch.Id, formType.Id);
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

                            var price = priceIndex[batch.Id];

                            item.PrimaryCurrencyPrice = price.TotalPriceInPrimaryCurrency;
                            if (batch.PriceConversionId != null)
                            {
                                item.SourceCurrencyPrice = batch.PriceConversion.SourceValue;
                                item.SourceCurrencyId = batch.PriceConversion.SourceCurrencyId;
                                item.ConversionId = batch.PriceConversion.Id;
                            }

                            CustomizeItemMapping(form, item, batch, context);
                        });
                }
            }
        }

        protected abstract void CustomizeItemMapping(IInvoiceForm form, IInvoiceFormItem item, IMaterialBatch batch, IInvoiceFormGenerationContext context);

        protected abstract void CustomizeFormMapping(IMaterialBatch referenceBatch,
            IInvoiceForm form,
            IInvoiceFormGenerationContext context);

        protected abstract IEnumerable<IMaterialBatch> FindSourceBatches(IMaterialInventory forInventory, int year,
            int month,
            IMaterialBatchFacade facade,
            IInvoiceFormGenerationContext context);
    }
}
