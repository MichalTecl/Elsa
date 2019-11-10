using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Accounting.InvoiceFormItemBridges;
using Elsa.Core.Entities.Commerce.Extensions;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Core.Data;
using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Invoicing.ReleasingFormsGeneration.Generators
{
    public class BatchCompositionReleaseFormsGenerator : ReleaseFormsGeneratorBase<BatchCompositionReleaseFormsGenerator.ManufacturingReleseEventDescriptor>
    {
        private readonly IMaterialBatchRepository m_batchRepository;
        private readonly IDatabase m_database;

        public BatchCompositionReleaseFormsGenerator(IMaterialBatchFacade batchFacade,
            IInvoiceFormsRepository invoiceFormsRepository, IMaterialBatchRepository batchRepository,
            IDatabase database, IMaterialRepository materialRepo) : base(
            batchFacade, invoiceFormsRepository, materialRepo)
        {
            m_batchRepository = batchRepository;
            m_database = database;
        }
        
        protected override void GenerateItems(IMaterialInventory forInventory, int year, int month, IInvoiceFormGenerationContext context,
            IReleasingFormsGenerationTask task, Action<DateTime, IMaterialBatch, Amount, ManufacturingReleseEventDescriptor> itemCallback)
        {
            CreateItemsByBatchComposition(forInventory, year, month, itemCallback);
        }

        private void CreateItemsByBatchComposition(IMaterialInventory forInventory, int year, int month, Action<DateTime, IMaterialBatch, Amount, ManufacturingReleseEventDescriptor> itemCallback)
        {
            var allBatchesWithComponentsFromThisInventory =
                m_batchRepository.GetBatchesByComponentInventory(forInventory.Id, year, month).Where(b => b.Batch.IsHiddenForAccounting != true).ToList();

            foreach (var composition in allBatchesWithComponentsFromThisInventory.Select(c => c.Batch))
            {
                foreach (var componentRecord in composition.Components)
                {
                    var componentBatch = m_batchRepository.GetBatchById(componentRecord.ComponentId).Batch;
                    if (componentBatch.Material.InventoryId != forInventory.Id)
                    {
                        continue;
                    }

                    itemCallback(composition.Created, componentBatch,
                        new Amount(componentRecord.Volume, componentRecord.Unit), new ManufacturingReleseEventDescriptor()
                        {
                            BatchCompositionRecordId = componentRecord.Id,
                            CompositionBatchText = composition.GetTextInfo()
                        });
                }
            }
        }
        
        protected override string GetGroupingKey(ItemReleaseModel item)
        {
            return $"{item.Date.Date}:{item.Descriptor.CompositionBatchText}";
        }

        protected override string GetExplanation(List<ItemReleaseModel> item, IInvoiceForm invoiceForm)
        {
            return $"Výroba šarže {item[0].Descriptor.CompositionBatchText}";
        }

        protected override void OnAfterItemSaved(IInvoiceForm form, IInvoiceFormItem item, ItemReleaseModel releaseModel)
        {
            if (releaseModel.Descriptor.BatchCompositionRecordId != null)
            {
                m_database.Save(m_database.New<IMaterialBatchCompositionFormItem>(i =>
                {
                    i.InvoiceFormItemId = item.Id;
                    i.MaterialBatchCompositionId = releaseModel.Descriptor.BatchCompositionRecordId.Value;
                }));
            } 
        }

        public class ManufacturingReleseEventDescriptor
        {
            public int? BatchCompositionRecordId { get; set; }
            
            public string CompositionBatchText { get; set; }
        }
    }
}
