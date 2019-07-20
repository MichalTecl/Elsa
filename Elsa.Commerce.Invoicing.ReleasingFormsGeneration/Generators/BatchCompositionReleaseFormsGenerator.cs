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
using Elsa.Core.Entities.Commerce.Inventory.ProductionSteps;
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
            CreateItemsByProductionSteps(forInventory, year, month, itemCallback);
        }

        private void CreateItemsByBatchComposition(IMaterialInventory forInventory, int year, int month, Action<DateTime, IMaterialBatch, Amount, ManufacturingReleseEventDescriptor> itemCallback)
        {
            var allBatchesWithComponentsFromThisInventory =
                m_batchRepository.GetBatchesByComponentInventory(forInventory.Id, year, month).ToList();

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

        private void CreateItemsByProductionSteps(IMaterialInventory forInventory, int year, int month, Action<DateTime, IMaterialBatch, Amount, ManufacturingReleseEventDescriptor> itemCallback)
        {
            DateUtil.GetMonthDt(year, month, out var dtFrom, out var dtTo);

            var batchesWithStepMaterialFromThisInventory = m_batchRepository
                .GetBatchesByProductionStepComponentInventory(forInventory.Id, year, month).ToList();

            foreach (var targetBatchId in batchesWithStepMaterialFromThisInventory)
            {
                var performedSteps = m_batchRepository.GetPerformedSteps(targetBatchId);

                IMaterialBatch targetBatch = null;

                foreach (var step in performedSteps)
                {
                    if (step.ConfirmDt < dtFrom || step.ConfirmDt > dtTo)
                    {
                        continue;
                    }

                    foreach (var sourceBatchBridge in step.SourceBatches)
                    {
                        if (sourceBatchBridge.SourceBatch.Material.InventoryId != forInventory.Id)
                        {
                            continue;
                        }

                        targetBatch = targetBatch ??
                                      (targetBatch = m_batchRepository.GetBatchById(targetBatchId).Batch);

                        itemCallback(step.ConfirmDt.Date, sourceBatchBridge.SourceBatch, new Amount(sourceBatchBridge.UsedAmount, sourceBatchBridge.Unit), new ManufacturingReleseEventDescriptor()
                        {
                            CompositionBatchText = targetBatch.GetTextInfo(),
                            ProdStepSourceBatchRecordId = sourceBatchBridge.Id
                        });
                    }
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
            } else if (releaseModel.Descriptor.ProdStepSourceBatchRecordId != null)
            {
                m_database.Save(m_database.New<IBatchStepBatchInvoiceItem>(i =>
                    {
                        i.InvoiceFormItemId = item.Id;
                        i.BatchProductionStepSourceBatchId = releaseModel.Descriptor.ProdStepSourceBatchRecordId.Value;
                    }));
            }
        }

        public class ManufacturingReleseEventDescriptor
        {
            public int? BatchCompositionRecordId { get; set; }

            public int? ProdStepSourceBatchRecordId { get; set; }

            public string CompositionBatchText { get; set; }
        }
    }
}
