using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.SaleEvents;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Commerce.SaleEvents;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Core.Data;
using RobOrm.Core;

namespace Elsa.Commerce.Invoicing.ReleasingFormsGeneration.Generators
{
    public class DirectSalesGenerator : ReleaseFormsGeneratorBase<DirectSaleDescriptor>
    {
        private readonly IMaterialRepository _materialRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IDatabase _database;
        private readonly ISaleEventRepository _saleEventRepository;

        public DirectSalesGenerator(IMaterialBatchFacade batchFacade, IInvoiceFormsRepository invoiceFormsRepository, IMaterialRepository materialRepository, IUnitRepository unitRepository, IDatabase database, ISaleEventRepository saleEventRepository) : base(batchFacade, invoiceFormsRepository, materialRepository)
        {
            _materialRepository = materialRepository;
            _unitRepository = unitRepository;
            _database = database;
            _saleEventRepository = saleEventRepository;
        }

        protected override string GetExplanation(List<ItemReleaseModel> item, IInvoiceForm invoiceForm)
        {
            return item[0].Descriptor.Event.Name;
        }

        protected override void GenerateItems(IMaterialInventory forInventory, int year, int month, IInvoiceFormGenerationContext context,
            IReleasingFormsGenerationTask task, Action<DateTime, IMaterialBatch, Amount, DirectSaleDescriptor> itemCallback)
        {
            if (!forInventory.CanBeConnectedToTag)
            {
                context.Info($"Pro sklad \"{forInventory.Name}\" se výdejky typu \"Přímý prodej\" negenerují - přeskakuji");
                return;
            }

            if (forInventory.AllowedUnitId == null)
            {
                throw new InvalidOperationException($"Pro sklad \"{forInventory.Name}\" musi byt nastavena dovolena merna jednotka");
            }

            var unit = _unitRepository.GetUnit(forInventory.AllowedUnitId.Value);

            DateUtil.GetMonthDt(year, month, out var fromDt, out var toDt);

            var events = _saleEventRepository.GetEvents(fromDt, toDt);

            foreach (var evt in events)
            {
                foreach (var allocation in evt.Allocations.Where(e => e.ReturnDt != null))
                {
                    var soldAmount = new Amount(allocation.AllocatedQuantity - allocation.ReturnedQuantity ?? 0m, allocation.Unit);
                    itemCallback(evt.EventDt, allocation.Batch, soldAmount, new DirectSaleDescriptor()
                    {
                        Event = evt,
                        Allocation = allocation
                    });
                }
            }
        }

        protected override string GetGroupingKey(ItemReleaseModel item)
        {
            return item.Descriptor.Event.Id.ToString();
        }
    }

    public class DirectSaleDescriptor
    {
        public ISaleEvent Event { get; set; }
        public ISaleEventAllocation Allocation { get; set; }
    }
}
