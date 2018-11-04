using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.App.OrdersPacking.Model;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Shipment;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
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
        private readonly IPurchaseOrderRepository m_orderRepository;
        private readonly IShipmentProvider m_shipmentProvider;
        private readonly IOrdersFacade m_ordersFacade;
        private readonly IKitProductRepository m_kitProductRepository;
        private readonly IErpClientFactory m_erpClientFactory;
        private readonly IMaterialBatchFacade m_batchFacade;
        private readonly IDatabase m_database; 
        
        public PackingController(IWebSession webSession, ILog log, IPurchaseOrderRepository orderRepository, IShipmentProvider shipmentProvider, IOrdersFacade ordersFacade, IKitProductRepository kitProductRepository, IErpClientFactory erpClientFactory, IMaterialBatchFacade batchFacade, IDatabase database)
            : base(webSession, log)
        {
            m_orderRepository = orderRepository;
            m_shipmentProvider = shipmentProvider;
            m_ordersFacade = ordersFacade;
            m_kitProductRepository = kitProductRepository;
            m_erpClientFactory = erpClientFactory;
            m_batchFacade = batchFacade;
            m_database = database;
        }

        public PackingOrderModel FindOrder(string number)
        {
            number = number.Trim();
            if (number.Length < 3)
            {
                throw new Exception("Musí být alespoň tři čísla");
            }

            m_orderRepository.PreloadOrders(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(1));

            var paid =
                m_orderRepository.GetOrdersByStatus(
                    OrderStatus.ReadyToPack,
                    DateTime.Now.AddDays(-30),
                    DateTime.Now.AddDays(1)).ToList();

            var filtered = paid.Where(o => GetTrackingNumber(o).EndsWith(number)).ToList();
            if (filtered.Count > 1)
            {
                throw new Exception("Objednávku nelze jednoznačně určit, použijte celé číslo nebo trasovací číslo Zásilkovny");
            }

            if (filtered.Count == 0)
            {
                Log.Info("Číslo objednávky nenalezeno");
                Log.Info("Spojuji se se Zásilkovnou...");
                var orderNumber = m_shipmentProvider.GetOrderNumberByPackageNumber(number);
                if (string.IsNullOrWhiteSpace(orderNumber))
                {
                    throw new Exception("Objednávka nebyla nalezena");
                }
                
                var order = paid.FirstOrDefault(o => o.PreInvoiceId == orderNumber || o.OrderNumber == orderNumber);
                if (order == null)
                {
                    Log.Info("Aplikace nemá načtenu tuto zásilku, je třeba aktualizovat seznam zásilek z Floxu...");

                    paid = m_ordersFacade.GetAndSyncPaidOrders(DateTime.Now.AddDays(-30)).ToList();


                    order = paid.FirstOrDefault(o => o.PreInvoiceId == orderNumber || o.OrderNumber == orderNumber);
                    if (order == null)
                    {
                        Log.Error("Nenalezeno");
                        throw new Exception($"Trasovací číslo bylo nalezeno v Zásilkovně pro objednávku č. {orderNumber}, která ale není mezi objednávkami k zabalení. Zkontrolujte objednávku ve FLOXu.");
                    }
                }

                Log.Info("Objednávka nalezena");

                return MapOrder(order);
            }

            return MapOrder(filtered.Single());
        }

        public PackingOrderModel SelectKitItem(long orderId, long orderItemId, int kitItemId, int kitItemIndex)
        {
            using (var tx = m_database.OpenTransaction())
            {
                var order = m_orderRepository.GetOrder(orderId);
                if (order == null)
                {
                    throw new InvalidOperationException("Objednavka nenalezena");
                }

                var item = order.Items.FirstOrDefault(i => i.Id == orderItemId);
                if (item == null)
                {
                    throw new InvalidOperationException("Polozka objednavky nenalezena");
                }

                m_kitProductRepository.SetKitItemSelection(order, item, kitItemId, kitItemIndex);

                var result = MapOrder(order);

                tx.Commit();

                return result;
            }
        }
        
        public void PackOrder(long orderId, List<OrderItemBatchAssignmentModel> batchAssignment)
        {
            using (var tx = m_database.OpenTransaction())
            {
                var order = m_orderRepository.GetOrder(orderId);
                if (order == null)
                {
                    throw new InvalidOperationException("Objednavka nenalezena");
                }

                var mapped = MapOrder(order);

                foreach (var item in mapped.Items)
                {
                    foreach (var kit in item.KitItems)
                    {
                        if (kit.GroupItems.Any() && kit.SelectedItem == null)
                        {
                            throw new InvalidOperationException(
                                      $"Objednavka nemuze byt dokoncena - ve skupine '{kit.GroupName}' neni vybrana polozka");
                        }
                    }
                }

                m_ordersFacade.SetOrderSent(orderId, batchAssignment);

                tx.Commit();
            }
        }

        public IEnumerable<LightOrderInfo> GetOrdersToPack()
        {
            return m_ordersFacade.GetAndSyncPaidOrders(DateTime.Now.AddDays(-30), true).Select(i => new LightOrderInfo(i));
        }

        private PackingOrderModel MapOrder(IPurchaseOrder entity)
        {
            var batchAssignments = m_batchFacade.CreateBatchesAssignmentProposal(entity).ToList();

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

            foreach (var sourceItem in entity.Items)
            {
                var item = new PackingOrderItemModel
                               {
                                   ProductName = sourceItem.PlacedName,
                                   ItemId = sourceItem.Id,
                                   Quantity = StringUtil.FormatDecimal(sourceItem.Quantity)
                               };

                var kitItems = new List<KitItemsCollectionModel>();

                foreach (var sourceKitItem in m_kitProductRepository.GetKitForOrderItem(entity, sourceItem))
                {
                    var model = new KitItemsCollectionModel(sourceKitItem);
                    if (sourceKitItem.SelectedItem != null)
                    {
                        var selitem = new PackingOrderItemModel
                        {
                            ProductName = sourceKitItem.SelectedItem.PlacedName,
                            ItemId = sourceKitItem.SelectedItem.Id,
                            Quantity = StringUtil.FormatDecimal(sourceKitItem.SelectedItem.Quantity)
                        };
                        selitem.BatchAssignment.AddRange(batchAssignments.Where(a => a.OrderItemId == sourceKitItem.SelectedItem.Id));
                        model.SelectedItemModel = selitem;
                    }
                    
                    kitItems.Add(model);
                }

                //var kitItems = m_kitProductRepository.GetKitForOrderItem(entity, sourceItem).Select().ToList();
                
                item.KitItems.AddRange(kitItems);
                item.BatchAssignment.AddRange(batchAssignments.Where(a => a.OrderItemId == sourceItem.Id));

                orderModel.Items.Add(item);
            }

            return orderModel;
        }

        private string GetTrackingNumber(IPurchaseOrder order)
        {
            if (order.ErpId == null)
            {
                throw new InvalidOperationException($"Neocekavany stav objednavky '{order.OrderNumber}' - objednavka nema prirazeny ERP system");
            }

            var erpClient = m_erpClientFactory.GetErpClient(order.ErpId.Value);

            return erpClient.GetPackingReferenceNumber(order);
        }

    }
}
