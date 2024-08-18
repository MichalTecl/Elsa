using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.App.OrdersPacking.Model;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Configuration;
using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Shipment;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;

using Robowire.RoboApi;
using Robowire.RobOrm.Core;

namespace Elsa.App.OrdersPacking
{
    [Controller("ordersPacking")]
    public class PackingController : ElsaControllerBase
    {
        private readonly IPurchaseOrderRepository _orderRepository;
        private readonly IShipmentProvider _shipmentProvider;
        private readonly IOrdersFacade _ordersFacade;
        private readonly IKitProductRepository _kitProductRepository;
        private readonly IErpClientFactory _erpClientFactory;
        private readonly IMaterialBatchFacade _batchFacade;
        private readonly IVirtualProductFacade _virtualProductFacade;
        private readonly IDatabase _database;
        private readonly OrdersSystemConfig _config;

        public PackingController(
            IWebSession webSession,
            ILog log,
            IPurchaseOrderRepository orderRepository,
            IShipmentProvider shipmentProvider,
            IOrdersFacade ordersFacade,
            IKitProductRepository kitProductRepository,
            IErpClientFactory erpClientFactory,
            IMaterialBatchFacade batchFacade,
            IDatabase database,
            IVirtualProductFacade virtualProductFacade, OrdersSystemConfig config)
            : base(webSession, log)
        {
            _orderRepository = orderRepository;
            _shipmentProvider = shipmentProvider;
            _ordersFacade = ordersFacade;
            _kitProductRepository = kitProductRepository;
            _erpClientFactory = erpClientFactory;
            _batchFacade = batchFacade;
            _database = database;
            _virtualProductFacade = virtualProductFacade;
            _config = config;
        }
        
        public PackingOrderModel FindOrder(string number)
        {
            WebSession.EnsureUserRight(OrdersPackingUserRights.OpenOrderPackingApplication);

            number = number.Trim();
            if (number.Length < 3)
            {
                throw new Exception("Musí být alespoň tři čísla");
            }
                       
            var seek = _orderRepository.SearchOrder(number, OrderStatus.ReadyToPack.Id);

            IPurchaseOrder filtered = null;
            if (seek != null)
            {
                filtered = _orderRepository.GetOrder(seek.Value);
            }

            if (filtered == null)
            {
                Log.Info("Číslo objednávky nenalezeno");
                Log.Info("Spojuji se se Zásilkovnou...");
                var orderNumber = _shipmentProvider.GetOrderNumberByPackageNumber(number);
                if (string.IsNullOrWhiteSpace(orderNumber))
                {
                    throw new Exception($"Objednávka {number} nebyla nalezena");
                }

                var paid =
                    _orderRepository.GetOrdersByStatus(
                        OrderStatus.ReadyToPack,
                        DateTime.Now.AddDays(-90),
                        DateTime.Now.AddDays(1)).ToList();

                var order = paid.FirstOrDefault(o => (o.PreInvoiceId == orderNumber) || (o.OrderNumber == orderNumber));
                if (order == null)
                {
                    Log.Info("Aplikace nemá načtenu tuto zásilku, je třeba aktualizovat seznam zásilek z Floxu...");

                    paid = _ordersFacade.GetAndSyncPaidOrders(DateTime.Now.AddDays(-30), shipProvider: null).ToList();

                    order = paid.FirstOrDefault(o => (o.PreInvoiceId == orderNumber) || (o.OrderNumber == orderNumber));
                    if (order == null)
                    {
                        Log.Error("Nenalezeno");
                        throw new Exception($"Trasovací číslo bylo nalezeno v Zásilkovně pro objednávku č. {orderNumber}, která ale není mezi objednávkami k zabalení. Zkontrolujte objednávku ve FLOXu.");
                    }
                }

                Log.Info("Objednávka nalezena");

                return MapOrder(order);
            }

            return MapOrder(filtered);
        }

        public PackingOrderModel SelectKitItem(long orderId, long orderItemId, int kitItemId, int kitItemIndex)
        {
            using (var tx = _database.OpenTransaction())
            {
                var order = _orderRepository.GetOrder(orderId);
                if (order == null)
                {
                    throw new InvalidOperationException("Objednavka nenalezena");
                }

                var item = order.Items.FirstOrDefault(i => i.Id == orderItemId);
                if (item == null)
                {
                    throw new InvalidOperationException("Polozka objednavky nenalezena");
                }

                _kitProductRepository.SetKitItemSelection(order, item, kitItemId, kitItemIndex);

                var result = MapOrder(order);

                tx.Commit();

                return result;
            }
        }
        
        public void PackOrder(long orderId)
        {
            WebSession.EnsureUserRight(OrdersPackingUserRights.MarkOrderPacked);

            using (var tx = _database.OpenTransaction())
            {
                var blocker = _orderRepository.TryGetProcessBlockMessage(orderId, "Packing");
                if (!string.IsNullOrWhiteSpace(blocker))
                {
                    throw new InvalidOperationException($"Objednavku nelze zabalit: {blocker}");
                }

                var order = _orderRepository.GetOrder(orderId);
                if (order == null)
                {
                    throw new InvalidOperationException("Objednavka nenalezena");
                }
                
                var mapped = MapOrder(order);

                foreach (var item in mapped.Items)
                {
                    foreach (var kit in item.KitItems)
                    {
                        if (kit.GroupItems.Any() && (kit.SelectedItem == null))
                        {
                            throw new InvalidOperationException(
                                      $"Objednavka nemuze byt dokoncena - ve skupine '{kit.GroupName}' neni vybrana polozka");
                        }
                    }

                    ValidateItemBatches(item);
                }

                if (_config.MarkOrdersSentAsync)
                {
                    _ordersFacade.SetOrderSentAsync(orderId);
                }
                else
                {
                    _ordersFacade.SetOrderSent(orderId);
                }

                tx.Commit();
            }
        }

        public PackingOrderModel SetItemBatchAllocation(BatchAllocationChangeRequest request)
        {
            WebSession.EnsureUserRight(OrdersPackingUserRights.OrderBatchAssignment);

            PackingOrderModel result;
            using (var tx = _database.OpenTransaction())
            {
                var order = _orderRepository.GetOrder(request.OrderId);
                if (order == null)
                {
                    throw new InvalidOperationException("Objednávka nebyla v systému nalezena");
                }

                if (!string.IsNullOrWhiteSpace(request.OriginalBatchNumber))
                {
                    _batchFacade.ChangeOrderItemBatchAssignment(
                        order,
                        request.OrderItemId,
                        request.OriginalBatchNumber,
                        request.NewAmount);
                    
                    order = _orderRepository.GetOrder(order.Id);

                    if (request.NewAmount > 0m)
                    {
                        request.NewBatchSearchQuery = request.NewBatchSearchQuery ?? request.OriginalBatchNumber;
                    }
                }

                var item = _ordersFacade.GetAllConcreteOrderItems(order).FirstOrDefault(i => i.Id == request.OrderItemId).Ensure();

                if (!string.IsNullOrWhiteSpace(request.NewBatchSearchQuery) && ((request.NewAmount ?? item.Quantity) > 0m))
                {
                    var material = _virtualProductFacade.GetOrderItemMaterialForSingleUnit(order, item);
                    var batch = _batchFacade.FindBatchBySearchQuery(material.MaterialId, request.NewBatchSearchQuery);

                    result = MapOrder(
                        order,
                        new Tuple<long, BatchKey, decimal>(item.Id, batch, request.NewAmount ?? item.Quantity));
                }
                else
                {
                    result = MapOrder(order);
                }

                tx.Commit();

                return result;
            }
        }

        public IEnumerable<LightOrderInfo> GetOrdersToPack()
        {
            if (!WebSession.HasUserRight(OrdersPackingUserRights.ViewOrdersPackingWidget))
            {
                return new LightOrderInfo[0];
            }

            return _ordersFacade.GetAndSyncPaidOrders(DateTime.Now.AddDays(-30), shipProvider: null, true).Select(i => new LightOrderInfo(i));
        }

        public OrderAsyncFieldModel<string> GetMostRecentInternalNote(long orderId)
        {
            var elsaOrder = _orderRepository.GetOrder(orderId);
            if (elsaOrder == null)
            {
                return null;
            }
            
            var erp = _erpClientFactory.GetErpClient(elsaOrder.ErpId.Value);

            var erpOrder = erp.LoadOrder(elsaOrder.OrderNumber);
            return new Model.OrderAsyncFieldModel<string>
            {
                OrderId = orderId,
                FieldName = "InternalNote",
                FieldValue = erpOrder.InternalNote
            };
        }

        private PackingOrderModel MapOrder(IPurchaseOrder entity, Tuple<long, BatchKey, decimal> orderItemBatchPreference = null)
        {
            entity = _ordersFacade.ResolveSingleItemKitSelection(entity);

            var batchAssignments = _batchFacade.TryResolveBatchAssignments(entity, orderItemBatchPreference).ToList();

            var orderModel = new PackingOrderModel
                                 {
                                     OrderId = entity.Id,
                                     OrderNumber = entity.OrderNumber,
                                     CustomerEmail = entity.CustomerEmail,
                                     CustomerName = entity.CustomerName,
                                     CustomerNote = entity.CustomerNote,
                                     InternalNote = entity.InternalNote,
                                     ErpName = entity.Erp?.Description,
                                     DiscountsText = entity.DiscountsText,
                                     Price = $"{StringUtil.FormatDecimal(entity.PriceWithVat)} {entity.Currency.Symbol}"
                                 };
            
            foreach (var sourceItem in entity.Items.OrderBy(i => i.Id))
            {
                var item = new PackingOrderItemModel
                               {
                                   ProductName = sourceItem.PlacedName,
                                   ItemId = sourceItem.Id,
                                   Quantity = StringUtil.FormatDecimal(sourceItem.Quantity),
                                   NumericQuantity = sourceItem.Quantity
                               };

                var kitItems = new List<KitItemsCollectionModel>();
                
                foreach (var sourceKitItem in _kitProductRepository.GetKitForOrderItem(entity, sourceItem).OrderBy(ki => ki.KitItemIndex))
                {
                    var model = new KitItemsCollectionModel(sourceKitItem);
                    
                    if (sourceKitItem.SelectedItem != null)
                    {
                        var selitem = new PackingOrderItemModel
                        {
                            ProductName = sourceKitItem.SelectedItem.PlacedName,
                            ItemId = sourceKitItem.SelectedItem.Id,
                            Quantity = StringUtil.FormatDecimal(sourceKitItem.SelectedItem.Quantity),
                            NumericQuantity = sourceKitItem.SelectedItem.Quantity
                        };
                        SetupBatchAssignment(selitem, batchAssignments.Where(a => a.OrderItemId == sourceKitItem.SelectedItem.Id));
                        model.SelectedItemModel = selitem;
                    }
                    
                    kitItems.Add(model);
                }
                
                item.KitItems.AddRange(kitItems);
                SetupBatchAssignment(item, batchAssignments.Where(a => a.OrderItemId == sourceItem.Id));
                orderModel.Items.Add(item);
            }

            return orderModel;
        }

        private void SetupBatchAssignment(
            PackingOrderItemModel item,
            IEnumerable<OrderItemBatchAssignmentModel> assignments)
        {
            var models = new List<BatchAssignmentViewModel>();

            foreach (var assignment in assignments)
            {
                var viewModel = models.FirstOrDefault(m =>
                    m.BatchNumber.Equals(assignment.BatchNumber, StringComparison.InvariantCultureIgnoreCase));
                if (viewModel == null)
                {
                    viewModel = new BatchAssignmentViewModel(assignment);
                    models.Add(viewModel);
                }
                else
                {
                    viewModel.Add(assignment);
                }
            }
            
            foreach (var model in models)
            {
                model.CanSplit = item.NumericQuantity > 1m;
                model.IsSplit = models.Count > 1;
            }

            item.BatchAssignment.AddRange(models);
        }

        private string GetTrackingNumber(IPurchaseOrder order)
        {
            if (order.ErpId == null)
            {
                throw new InvalidOperationException($"Neocekavany stav objednavky '{order.OrderNumber}' - objednavka nema prirazeny ERP system");
            }

            var erpClient = _erpClientFactory.GetErpClient(order.ErpId.Value);

            return erpClient.GetPackingReferenceNumber(order);
        }

        private void ValidateItemBatches(PackingOrderItemModel item)
        {
            if (item.KitItems.Any())
            {
                foreach (var kitItem in item.KitItems.Where(i => i.SelectedItemModel != null))
                {
                    ValidateItemBatches(kitItem.SelectedItemModel);
                }

                return;
            }

            ValidateItemBatches(item.ProductName, item.NumericQuantity, item.BatchAssignment);
        }

        private void ValidateItemBatches(
            string productName,
            decimal itemQuantity,
            IEnumerable<BatchAssignmentViewModel> assignments)
        {
            var allocatonSum = assignments.Where(i => i.MaterialBatchId > 0).Sum(i => i.AssignedQuantity);
            if (Math.Abs(allocatonSum - itemQuantity) > 0.00001m)
            {
                throw new InvalidOperationException($"Neplatné přiřazení šarže k položce '{productName}'. Rozdíl požadované a skutečné hodnoty je {allocatonSum - itemQuantity}");
            }
        }
    }
}
