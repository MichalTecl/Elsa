using System;
using System.Linq;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Core.Data;
using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Invoicing.ReleasingFormsGeneration.Generators
{
    public class BatchCompositionReleaseFormsGenerator : ReleaseFormsGeneratorBase<int>
    {
        private readonly IMaterialBatchRepository m_batchRepository;
        private readonly IDatabase m_database;

        public BatchCompositionReleaseFormsGenerator(IMaterialBatchFacade batchFacade,
            IInvoiceFormsRepository invoiceFormsRepository, IMaterialBatchRepository batchRepository,
            IDatabase database) : base(
            batchFacade, invoiceFormsRepository)
        {
            m_batchRepository = batchRepository;
            m_database = database;
        }

        protected override string FormText => "VÝROBA";

        protected override void GenerateItems(IMaterialInventory forInventory, int year, int month, IInvoiceFormGenerationContext context,
            IReleasingFormsGenerationTask task, Action<DateTime, IMaterialBatch, Amount, int> itemCallback)
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

                    itemCallback(composition.Created.Date, componentBatch,
                        new Amount(componentRecord.Volume, componentRecord.Unit), componentRecord.Id);
                }
            }
        }

        protected override void OnAfterItemSaved(IInvoiceForm form, IInvoiceFormItem item, ItemReleaseModel releaseModel)
        {
            if (releaseModel.Descriptor > 0)
            {
                m_database.Save(m_database.New<IMaterialBatchCompositionFormItem>(i =>
                {
                    i.InvoiceFormItemId = item.Id;
                    i.MaterialBatchCompositionId = releaseModel.Descriptor;
                }));
            }
        }
    }
}
