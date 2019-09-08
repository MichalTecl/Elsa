using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.StockEvents;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Core.Data;

namespace Elsa.Commerce.Invoicing.ReleasingFormsGeneration.Generators
{
    public class ReturnedOrdersFormGenerator : ReleaseFormsGeneratorBase<ReturnedOrderDescriptor>
    {
        private readonly IPurchaseOrderRepository m_orderRepository;
        private readonly IUnitRepository m_unitRepository;
        private readonly IMaterialRepository m_materialRepository;
        private readonly AmountProcessor m_amountProcessor;
        private readonly IStockEventRepository m_stockEventRepository;

        public ReturnedOrdersFormGenerator(IMaterialBatchFacade batchFacade, IInvoiceFormsRepository invoiceFormsRepository, IMaterialRepository materialRepository, IPurchaseOrderRepository orderRepository, IUnitRepository unitRepository, AmountProcessor amountProcessor, IStockEventRepository stockEventRepository) : base(batchFacade, invoiceFormsRepository, materialRepository)
        {
            m_materialRepository = materialRepository;
            m_orderRepository = orderRepository;
            m_unitRepository = unitRepository;
            m_amountProcessor = amountProcessor;
            m_stockEventRepository = stockEventRepository;
        }

        protected override string GetExplanation(List<ItemReleaseModel> item, IInvoiceForm invoiceForm)
        {
            return $"Vratka objednávky č.: {item[0].Descriptor.OrderNumber}";
        }

        protected override void GenerateItems(IMaterialInventory forInventory, int year, int month, IInvoiceFormGenerationContext context,
            IReleasingFormsGenerationTask task, Action<DateTime, IMaterialBatch, Amount, ReturnedOrderDescriptor> itemCallback)
        {
            if (!forInventory.CanBeConnectedToTag)
            {
                context.Info($"Pro sklad \"{forInventory.Name}\" se výdejky typu \"VRACENE OBJEDNAVKY\" negenerují - přeskakuji");
                return;
            }
            
            var returns = m_orderRepository.GetReturns(month, year).ToList();

            DateUtil.GetMonthDt(year, month, out var dtFrom, out var dtTo);

            foreach (var order in returns)
            {
                var allOrderItems = new List<IOrderItem>();

                foreach (var orderItem in order.Items)
                {
                    if (orderItem.AssignedBatches.Any())
                    {
                        allOrderItems.Add(orderItem);
                    }

                    foreach (var kitChild in orderItem.KitChildren)
                    {
                        if (kitChild.AssignedBatches.Any())
                        {
                            allOrderItems.Add(kitChild);
                        }
                    }
                }
                
                foreach (var item in allOrderItems)
                {
                    foreach (var assignment in item.AssignedBatches)
                    {
                        //todo: may be neccessary to filter by batch inventory in the future

                        var batchMaterial = m_materialRepository.GetMaterialById(assignment.MaterialBatch.MaterialId);
                        
                        itemCallback(order.ReturnDt ?? order.BuyDate, assignment.MaterialBatch, 
                            m_amountProcessor.Neg(new Amount(assignment.Quantity, batchMaterial.NominalUnit)), new ReturnedOrderDescriptor
                        {
                            OrderNumber = order.OrderNumber
                        });
                    }
                }

                foreach (var connectedEvent in m_stockEventRepository.GetEvents(dtFrom, dtTo, order.Id))
                {
                    itemCallback(order.ReturnDt ?? order.BuyDate, connectedEvent.Batch,
                        m_amountProcessor.Neg(new Amount(connectedEvent.Delta, connectedEvent.Unit)), new ReturnedOrderDescriptor
                        {
                            OrderNumber = order.OrderNumber
                        });
                }

            }
        }

        protected override string GetGroupingKey(ItemReleaseModel item)
        {
            return item.Descriptor.OrderNumber;
        }
    }

    public class ReturnedOrderDescriptor
    {
        public string OrderNumber { get; set; }
    }
}
