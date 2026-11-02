using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Accounting.InvoiceFormItemBridges;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Core.Data;
using RobOrm.Core;

namespace Elsa.Commerce.Invoicing.ReleasingFormsGeneration.Generators
{
    public class EshopSalesGenerator : ReleaseFormsGeneratorBase<EshopOrderDescriptor>
    {
        private readonly IPurchaseOrderRepository _orderRepository;
        private readonly IMaterialRepository _materialRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IDatabase _database;

        public EshopSalesGenerator(IMaterialBatchFacade batchFacade, IInvoiceFormsRepository invoiceFormsRepository,
            IPurchaseOrderRepository orderRepository, IMaterialRepository materialRepository,
            IUnitRepository unitRepository, IDatabase database) : base(batchFacade, invoiceFormsRepository, materialRepository)
        {
            _orderRepository = orderRepository;
            _materialRepository = materialRepository;
            _unitRepository = unitRepository;
            _database = database;
        }
        
        protected override string GetExplanation(List<ItemReleaseModel> item, IInvoiceForm invoiceForm)
        {
            return $"Objednávka č.: {item[0].Descriptor.OrderIdentifierText}";
        }

        protected override void GenerateItems(IMaterialInventory forInventory, int year, int month, IInvoiceFormGenerationContext context,
            IReleasingFormsGenerationTask task, Action<DateTime, IMaterialBatch, Amount, EshopOrderDescriptor> itemCallback)
        {
            if (!forInventory.CanBeConnectedToTag)
            {
                context.Info($"Pro sklad \"{forInventory.Name}\" se výdejky typu \"PRODEJ e-shop\" negenerují - přeskakuji");
                return;
            }

            if (forInventory.AllowedUnitId == null)
            {
                throw new InvalidOperationException($"Pro sklad \"{forInventory.Name}\" musi byt nastavena dovolena merna jednotka");
            }

            var unit = _unitRepository.GetUnit(forInventory.AllowedUnitId.Value);

            DateUtil.GetMonthDt(year, month, out var fromDt, out var toDt);

            _orderRepository.PreloadOrders(fromDt, toDt);

            var allOrders = _orderRepository.GetOrders(q => q.Where(o => o.BuyDate >= fromDt && o.BuyDate < toDt))
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
                            OrderIdentifierText = orderText,
                            OrderItemBatchAssignmentId = itemBatch.Id,
                            OrderInvoiceVarSymbol = order.VarSymbol
                        });
                    }

                    foreach (var kitChild in item.KitChildren)
                    {
                        foreach (var kitBatchBridge in kitChild.AssignedBatches)
                        {
                            itemCallback(order.BuyDate, kitBatchBridge.MaterialBatch, new Amount(kitBatchBridge.Quantity, unit), new EshopOrderDescriptor()
                            {
                                OrderIdentifierText = orderText,
                                OrderItemBatchAssignmentId = kitBatchBridge.Id,
                                OrderInvoiceVarSymbol = order.VarSymbol
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

        protected override void OnAfterItemSaved(IInvoiceForm form, IInvoiceFormItem item, ItemReleaseModel releaseModel)
        {
            _database.Save(_database.New<IOrderItemInvoiceFormItem>(i =>
            {
                i.InvoiceFormItemId = item.Id;
                i.BatchAssignmentId = releaseModel.Descriptor.OrderItemBatchAssignmentId;
            }));
        }

        protected override void CustomizeFormCreation(List<ItemReleaseModel> formItems, IInvoiceForm form)
        {
            form.InvoiceVarSymbol = formItems.Select(fi => fi.Descriptor.OrderInvoiceVarSymbol)
                                        .FirstOrDefault(vs => !string.IsNullOrWhiteSpace(vs)) ?? string.Empty;
        }
    }

    public class EshopOrderDescriptor
    {
        public string OrderIdentifierText { get; set; }

        public long OrderItemBatchAssignmentId { get; set; }

        public string OrderInvoiceVarSymbol { get; set; }
    }
}
