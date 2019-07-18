using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Core.Data;

namespace Elsa.Commerce.Invoicing.ReleasingFormsGeneration.Generators
{
    public class EshopSalesGenerator : ReleaseFormsGeneratorBase<EshopOrderDescriptor>
    {
        private readonly IPurchaseOrderRepository m_orderRepository;
        private readonly IMaterialRepository m_materialRepository;
        private readonly IUnitRepository m_unitRepository;

        public EshopSalesGenerator(IMaterialBatchFacade batchFacade, IInvoiceFormsRepository invoiceFormsRepository,
            IPurchaseOrderRepository orderRepository, IMaterialRepository materialRepository,
            IUnitRepository unitRepository) : base(batchFacade, invoiceFormsRepository, materialRepository)
        {
            m_orderRepository = orderRepository;
            m_materialRepository = materialRepository;
            m_unitRepository = unitRepository;
        }

        protected override string FormText => "Prodej E-Shop";

        protected override string GetExplanation(List<ItemReleaseModel> item, IInvoiceForm invoiceForm)
        {
            return $"Objednávka č.: {item[0].Descriptor.OrderIdentifierText}";
        }

        protected override void GenerateItems(IMaterialInventory forInventory, int year, int month, IInvoiceFormGenerationContext context,
            IReleasingFormsGenerationTask task, Action<DateTime, IMaterialBatch, Amount, EshopOrderDescriptor> itemCallback)
        {
            if (!forInventory.CanBeConnectedToTag)
            {
                context.Info($"Pro sklad \"{forInventory.Name}\" se výdejky typu \"{FormText}\" negenerují - přeskakuji");
                return;
            }

            if (forInventory.AllowedUnitId == null)
            {
                throw new InvalidOperationException($"Pro sklad \"{forInventory.Name}\" musi byt nastavena dovolena merna jednotka");
            }

            var unit = m_unitRepository.GetUnit(forInventory.AllowedUnitId.Value);

            DateUtil.GetMonthDt(year, month, out var fromDt, out var toDt);

            m_orderRepository.PreloadOrders(fromDt, toDt);

            var allOrders = m_orderRepository.GetOrders(q => q.Where(o => o.BuyDate >= fromDt && o.BuyDate < toDt))
                .ToList();

            foreach (var order in allOrders)
            {
                if (!OrderStatus.IsSent(order.OrderStatusId))
                {
                    continue;
                }

                var orderText = order.OrderNumber;

                foreach (var item in order.Items)
                {
                    foreach (var itemBatch in item.AssignedBatches)
                    {
                        itemCallback(order.BuyDate, itemBatch.MaterialBatch, new Amount(itemBatch.Quantity, unit), new EshopOrderDescriptor()
                        {
                            OrderIdentifierText = orderText
                        });
                    }

                    foreach (var kitChild in item.KitChildren)
                    {
                        foreach (var kitBatchBridge in kitChild.AssignedBatches)
                        {
                            itemCallback(order.BuyDate, kitBatchBridge.MaterialBatch, new Amount(kitBatchBridge.Quantity, unit), new EshopOrderDescriptor()
                            {
                                OrderIdentifierText = orderText
                            });
                        }
                    }
                }
            }
        }

        protected override string GetGroupingKey(ItemReleaseModel item)
        {
            return item.Descriptor.OrderIdentifierText;
        }
    }

    public class EshopOrderDescriptor
    {
        public string OrderIdentifierText { get; set; }
    }
}
