using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Integration;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Core.Entities.Commerce.Inventory.Kits;
using Elsa.Smtp.Core;
using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Impl
{
    public class OrdersFacade : IOrdersFacade
    {
        private readonly IPurchaseOrderRepository m_orderRepository;
        private readonly IDatabase m_database;
        private readonly IErpClientFactory m_clientFactory;
        private readonly ISession m_session;
        private readonly IPaymentRepository m_paymentRepository;
        private readonly ILog m_log;
        private readonly IMaterialBatchFacade m_batchFacade;
        private readonly IKitProductRepository m_kitProductRepository;
        private readonly IMailSender m_mailSender;
        private readonly ICache m_cache;

        public OrdersFacade(
            IPurchaseOrderRepository orderRepository,
            IDatabase database,
            IErpClientFactory clientFactory,
            ISession session,
            IPaymentRepository paymentRepository,
            ILog log,
            IMaterialBatchFacade batchFacade, IKitProductRepository kitProductRepository, IMailSender mailSender, ICache cache)
        {
            m_orderRepository = orderRepository;
            m_database = database;
            m_clientFactory = clientFactory;
            m_session = session;
            m_paymentRepository = paymentRepository;
            m_log = log;
            m_batchFacade = batchFacade;
            m_kitProductRepository = kitProductRepository;
            m_mailSender = mailSender;
            m_cache = cache;
        }

        public IPurchaseOrder SetOrderPaid(long orderId, long? paymentId)
        {
            var order = m_orderRepository.GetOrder(orderId);
            if (order == null)
            {
                throw new InvalidOperationException("Order not found");
            }

            if (paymentId != null)
            {
                var payment = m_paymentRepository.GetPayment(paymentId.Value);
                if (payment == null)
                {
                    throw new InvalidOperationException("Platba neexistuje");
                }

                var ordersList = payment.Orders?.ToList() ?? new List<IPurchaseOrder>(0);
                if ((ordersList.SingleOrDefault()?.Id ?? orderId) != orderId)
                {
                    throw new InvalidOperationException("Platba uz je prirazena k jine objednavce");
                }
            }

            using (var tx = m_database.OpenTransaction())
            {
                m_log.Info("Starting transaction for payment pairing orderId={orderId}, paymentId={paymentId}");
                if (order.ErpId != null)
                {
                    order.PaymentId = paymentId;
                    order.PaymentPairingUserId = m_session.User.Id;
                    order.PaymentPairingDt = DateTime.Now;
                    m_database.Save(order);
                    
                    m_log.Info($"Order saved to the database in transaction; paymentId={order.PaymentId}, pairngUserId={order.PaymentPairingUserId}, pairingDt={order.PaymentPairingDt}");
                }
                else
                {
                    m_log.Error("Order.ErpId == null");
                }

                if (order.OrderStatusId == OrderStatus.PendingPayment.Id)
                {
                    order = PerformErpActionSafe(order, (e, ord) => e.MarkOrderPaid(ord),
                        synced =>
                        {
                            if (!OrderStatus.IsPaid(synced.OrderStatusId))
                            {
                                throw new InvalidOperationException(
                                    $" Byl odeslan pozadavek na nastaveni platby objednavky, ale objednavka ma stale stav '{synced.ErpStatusId} - {synced.ErpStatusName}', ktery Elsa mapuje na stav '{synced.OrderStatus?.Name}'");
                            }
                        }, "SetOrderPaid");
                }
                else
                {
                    m_log.Info($"Order sync skipped - statusId={order.OrderStatusId}");
                }
                
                tx.Commit();
                m_log.Info("Transaction commited");
            }

            return order;
        }

        public IPurchaseOrder SetOrderSent(long orderId)
        {
            var order = m_orderRepository.GetOrder(orderId);
            if (order == null)
            {
                throw new InvalidOperationException("Order not found");
            }

            using (var tx = m_database.OpenTransaction())
            {
                order.PackingDt = DateTime.Now;
                order.PackingUserId = m_session.User.Id;

                foreach (var item in GetAllConcreteOrderItems(order))
                {
                    var assignments = GetAssignedBatches(item.Id).ToList();
                    if (assignments.Count == 0)
                    {
                        if (item.KitChildren.Any())
                        {
                            continue;
                        }

                        throw new InvalidOperationException($"Batch assignment missing - OrderItemId = {item.Id}");
                    }

                    var sum = assignments.Sum(a => a.Quantity);
                    if (Math.Abs(sum - item.Quantity) > 0.0001m)
                    {
                        throw new InvalidOperationException($"Invalid assignment quantity - OrderItemId={item.Id}, diff={Math.Abs(sum - item.Quantity)}");
                    }
                }

                if (order.ErpId == null)
                {
                    order.OrderStatusId = OrderStatus.Sent.Id;
                    m_database.Save(order);
                }
                else
                {
                    order = PerformErpActionSafe(
                        order,
                        (e, o) => e.MakeOrderSent(o),
                        synced =>
                            {
                                if (synced.OrderStatusId != OrderStatus.Sent.Id)
                                {
                                    throw new InvalidOperationException(
                                              $" Byl odeslan pozadavek na dokonceni objednavky, ale objednavka ma stale stav '{synced.ErpStatusId} - {synced.ErpStatusName}', ktery Elsa mapuje na stav '{synced.OrderStatus?.Name}'");
                                }
                            }, "SetOrderSent");
                }

                tx.Commit();

                return order;
            }
        }

        public void SetOrderSentAsync(long orderId)
        {
            var order = m_orderRepository.GetOrder(orderId);
            if (order == null)
            {
                throw new InvalidOperationException("Order not found");
            }

            using (var tx = m_database.OpenTransaction())
            {
                order.PackingDt = DateTime.Now;
                order.PackingUserId = m_session.User.Id;

                foreach (var item in GetAllConcreteOrderItems(order))
                {
                    var assignments = GetAssignedBatches(item.Id).ToList();
                    if (assignments.Count == 0)
                    {
                        if (item.KitChildren.Any())
                        {
                            continue;
                        }

                        throw new InvalidOperationException($"Batch assignment missing - OrderItemId = {item.Id}");
                    }

                    var sum = assignments.Sum(a => a.Quantity);
                    if (Math.Abs(sum - item.Quantity) > 0.0001m)
                    {
                        throw new InvalidOperationException($"Invalid assignment quantity - OrderItemId={item.Id}, diff={Math.Abs(sum - item.Quantity)}");
                    }
                }

                if (order.ErpId == null)
                {
                    order.OrderStatusId = OrderStatus.Sent.Id;
                    m_database.Save(order);
                }
                else
                {
                    Task.Run(() =>
                    {
                        try
                        {

                            order = PerformErpActionSafe(
                                order,
                                (e, o) => e.MakeOrderSent(o),
                                synced =>
                                {
                                    if (synced.OrderStatusId != OrderStatus.Sent.Id)
                                    {
                                        throw new InvalidOperationException(
                                            $" Byl odeslan pozadavek na dokonceni objednavky, ale objednavka ma stale stav '{synced.ErpStatusId} - {synced.ErpStatusName}', ktery Elsa mapuje na stav '{synced.OrderStatus?.Name}'");
                                    }
                                }, "SetOrderSent");
                        }
                        catch(Exception ex) 
                        {
                            m_log.Error($"Chyba pri posilani zabalene objednavky", ex);
                            m_mailSender.Send(order.PackingUser?.EMail ?? m_session.User.EMail, $"Chyba odesílání objednávky {order.OrderNumber} {order.CustomerName}",
                                $"Pozor - při dokončení balení objednávky {order.OrderNumber} {order.CustomerName} nastala chyba: '{ex.Message}'\r\nZkontrolujte objednávku ručně.");


                        }
                    });
                }

                tx.Commit();
            }
        }

        public IEnumerable<IPurchaseOrder> GetAndSyncPaidOrders(DateTime historyDepth, string shipProvider, bool skipErp = false)
        {
            m_log.Info($"Nacitam zaplacene objednavky od {historyDepth}");

            if (skipErp)
            {
                foreach (var rpo in m_orderRepository.GetOrdersByStatus(OrderStatus.ReadyToPack))
                {
                    if (!MatchShipmentProvider(rpo, shipProvider))
                        continue;

                    yield return rpo;
                }

                yield break;
            }


            var erps = m_clientFactory.GetAllErpClients().ToList();

            m_orderRepository.PreloadOrders(historyDepth, DateTime.Now.AddDays(1));

            foreach (var erpClient in erps)
            {
                m_log.Info($"Nacitam zaplacene objednavky od {historyDepth} z ERP = {erpClient.Erp.Description}");

                var paidOrders = erpClient.LoadPaidOrders(historyDepth, DateTime.Now.AddDays(1)).ToList();
                m_log.Info($"Stazeno {paidOrders.Count} objednavek");

                if (!paidOrders.Any())
                {
                    continue;
                }

                foreach (var paidOrder in paidOrders)
                {
                    var importedId = m_orderRepository.ImportErpOrder(paidOrder);
                    var order = m_orderRepository.GetOrder(importedId);

                    if (!MatchShipmentProvider(order, shipProvider))
                        continue;

                    yield return order;
                }
            }

            m_log.Info("Hotovo");
        }

        public IEnumerable<IOrderItem> GetAllConcreteOrderItems(IPurchaseOrder order)
        {
            foreach (var item in order.Items)
            {
                var kitItems = m_kitProductRepository.GetKitForOrderItem(order, item).ToList();

                if (!kitItems.Any())
                {
                    yield return item;
                    continue;
                }

                foreach (var kitItem in kitItems.Where(k => k.SelectedItem != null))
                {
                    yield return kitItem.SelectedItem;
                }
            }
        }

        public IEnumerable<Tuple<IPurchaseOrder, decimal>> GetOrdersByUsedBatch(BatchKey bqatch, int pageSize, int pageNumber)
        {
            var orderIds = new List<Tuple<long, decimal>>();

            m_database.Sql().Call("GetOrderIdsByUsedBatch").WithParam("@projectId", m_session.Project.Id)
                .WithParam("@materialId", bqatch.GetMaterialId(m_batchFacade))
                .WithParam("@batchNumber", bqatch.GetBatchNumber(m_batchFacade))
                .WithParam("@skip", pageSize * pageNumber).WithParam("@take", pageSize).ReadRows<long, int, string, decimal>(
                    (orderId, prio, orderNum, qty) =>
                    {
                        orderIds.Add(new Tuple<long, decimal>(orderId, qty));
                    });

            var entities = m_database.SelectFrom<IPurchaseOrder>()
                .Where(o => o.Id.InCsv(orderIds.Select(i => i.Item1)))
                .Execute().ToList();

            foreach (var id in orderIds)
            {
                var ett = entities.FirstOrDefault(e => e.Id == id.Item1);
                if (ett != null)
                {
                    yield return new Tuple<IPurchaseOrder, decimal>(ett, id.Item2);
                }
            }
        }

        public IPurchaseOrder ResolveSingleItemKitSelection(IPurchaseOrder entity)
        {
            var modified = false;

            foreach (var orderItem in entity.Items)
            {
                var kit = m_kitProductRepository.GetKitForOrderItem(entity, orderItem).ToList();
                if (!kit.Any())
                {
                    continue;
                }
                
                foreach (var kitItem in kit)
                {
                    var groupItems = kitItem.GroupItems.ToList();
                    if (kitItem.SelectedItem != null || groupItems.Count != 1)
                    {
                        // Item already selected or it should be chosen by the operator
                        continue;
                    }

                    m_kitProductRepository.SetKitItemSelection(entity, orderItem, groupItems.Single().Id, kitItem.KitItemIndex);
                    modified = true;
                }

            }

            if (!modified)
            {
                return entity;
            }

            return m_orderRepository.GetOrder(entity.Id);
        }

        private IPurchaseOrder PerformErpActionSafe(
            IPurchaseOrder order,
            Action<IErpClient, IPurchaseOrder> erpAction,
            Action<IPurchaseOrder> validateSyncedOrder,
            string actionLogDescription)
        {
            m_log.Info($"PerformErpActionSafe started: orderId={order.Id} orderNr={order.OrderNumber} action={actionLogDescription} statusId={order.OrderStatusId} erpStatus={order.ErpStatusName}");

            m_database.Save(order);
            
            var orderId = order.Id;

            if (order.ErpId == null)
            {
                throw new InvalidOperationException("Cannot perform ERP operation for order without ERP");
            }

            var erp = m_clientFactory.GetErpClient(order.ErpId.Value);

            try
            {
                erpAction(erp, order);
                m_log.Info($"PerformErpActionSafe - erpAction performed ok: orderId={order.Id} orderNr={order.OrderNumber} action={actionLogDescription}");
            }
            catch (Exception ex)
            {
                m_log.Error($"PerformErpActionSafe - erpAction failed: orderId={order.Id} orderNr={order.OrderNumber} action={actionLogDescription}", ex);
                throw;
            }

            m_log.Info($"Downloading order from ERP by orderNumber={order.OrderNumber}");

            var erpOrder = erp.LoadOrder(order.OrderNumber);
            if (erpOrder == null)
            {
                m_log.Error($"Downloading order from ERP by orderNumber={order.OrderNumber} failed!");

                throw new InvalidOperationException(
                            $"Nepodarilo se stahnout objednavku '{order.OrderNumber}' ze systemu '{order.Erp?.Description}'");
            }

            var importedOrderId = m_orderRepository.ImportErpOrder(erpOrder);
            if (importedOrderId != orderId)
            {
                throw new InvalidOperationException(
                            $"Chyba synchronizace objednavky '{order.OrderNumber}' ze systemu '{order.Erp?.Description}'");
            }

            order = m_orderRepository.GetOrder(importedOrderId);
            
            try
            {
                validateSyncedOrder(order);
            }
            catch (Exception ex)
            {
                m_log.Error($"Chyba pri pokusu o zpracovani objednavky '{order.OrderNumber}' po pozadavku na zmenu v systemu '{order.Erp?.Description}': {ex.Message}", ex);
                throw new InvalidOperationException($"Chyba pri pokusu o zpracovani objednavky '{order.OrderNumber}' po pozadavku na zmenu v systemu '{order.Erp?.Description}': {ex.Message}");
            }

            m_log.Info($"PerformErpActionSafe finished ok; order reloaded from database: orderId={order.Id} orderNr={order.OrderNumber} action={actionLogDescription} statusId={order.OrderStatusId} erpStatus={order.ErpStatusName}");

            return order;
        }

        private IEnumerable<IOrderItemMaterialBatch> GetAssignedBatches(long orderItemId)
        {
            var qry = m_database.SelectFrom<IOrderItemMaterialBatch>()
                .Join(ib => ib.MaterialBatch)
                .Where(ib => ib.OrderItemId == orderItemId);

            return qry.Execute();
        }

        private bool MatchShipmentProvider(IPurchaseOrder order, string shipmentProviderName) 
        {
            if (shipmentProviderName == null)
                return true;

            var lookup = m_cache.ReadThrough($"shipmentProviderLookup_{m_session.Project.Id}", TimeSpan.FromSeconds(10), () => {
                return m_database.SelectFrom<IShipmentProviderLookup>().Where(p => p.ProjectId == m_session.Project.Id).Execute().ToList();
            });

            var matching = lookup.Where(lkp => StringUtil.MatchStarWildcard(lkp.ShipMethodWildcardPattern, order.ShippingMethodName))
                .Select(l => l.ProviderName)
                .Distinct()
                .ToList();

            if (matching.Count != 1)
                throw new Exception($"U objednávky {order.OrderNumber} nelze určit dopravce. ShippingMethodName=\"{order.ShippingMethodName}\"; Nalezení dopravci:[{string.Join(",", matching)}]");

            return string.Equals(matching[0], shipmentProviderName, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
