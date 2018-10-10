using System;
using System.Linq;

using Elsa.App.OrdersPacking.Model;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Shipment;
using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;

using Robowire.RoboApi;

namespace Elsa.App.OrdersPacking
{
    [Controller("ordersPacking")]
    public class PackingController : ElsaControllerBase
    {
        private readonly IPurchaseOrderRepository m_orderRepository;
        private readonly IShipmentProvider m_shipmentProvider;
        private readonly IOrdersFacade m_ordersFacade;
        private readonly IKitProductRepository m_kitProductRepository;
        
        public PackingController(IWebSession webSession, ILog log, IPurchaseOrderRepository orderRepository, IShipmentProvider shipmentProvider, IOrdersFacade ordersFacade, IKitProductRepository kitProductRepository)
            : base(webSession, log)
        {
            m_orderRepository = orderRepository;
            m_shipmentProvider = shipmentProvider;
            m_ordersFacade = ordersFacade;
            m_kitProductRepository = kitProductRepository;
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

            var filtered = paid.Where(o => (o.PreInvoiceId?.EndsWith(number) ?? false) || (o.PreInvoiceId?.EndsWith(number) ?? false)).ToList();
            if (filtered.Count > 1)
            {
                throw new Exception("Objednávku nelze jednoznačně určit, použijte celé číslo objednávky nebo trasovací číslo Zásilkovny");
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

            return MapOrder(order);
        }

        public void PackOrder(long orderId)
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
                        throw new InvalidOperationException($"Objednavka nemuze byt dokoncena - ve skupine '{kit.GroupName}' neni vybrana polozka");
                    }
                }
            }

            m_ordersFacade.SetOrderSent(orderId);
        }

        private PackingOrderModel MapOrder(IPurchaseOrder entity)
        {
            var orderModel = new PackingOrderModel
                                 {
                                     OrderId = entity.Id,
                                     OrderNumber = entity.OrderNumber,
                                     CustomerEmail = entity.CustomerEmail,
                                     CustomerName = entity.CustomerName,
                                     CustomerNote = entity.CustomerNote,
                                     InternalNote = entity.InternalNote,
                                     ErpName = entity.Erp?.Description,
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

                item.KitItems.AddRange(m_kitProductRepository.GetKitForOrderItem(entity, sourceItem));

                orderModel.Items.Add(item);
            }

            return orderModel;
        }
    }
}
