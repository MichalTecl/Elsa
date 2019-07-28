using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Inventory.Kits;
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

        public OrdersFacade(
            IPurchaseOrderRepository orderRepository,
            IDatabase database,
            IErpClientFactory clientFactory,
            ISession session,
            IPaymentRepository paymentRepository,
            ILog log,
            IMaterialBatchFacade batchFacade, IKitProductRepository kitProductRepository)
        {
            m_orderRepository = orderRepository;
            m_database = database;
            m_clientFactory = clientFactory;
            m_session = session;
            m_paymentRepository = paymentRepository;
            m_log = log;
            m_batchFacade = batchFacade;
            m_kitProductRepository = kitProductRepository;
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

            order.PaymentId = paymentId;
            order.PaymentPairingUserId = m_session.User.Id;
            order.PaymentPairingDt = DateTime.Now;

            if (order.ErpId != null)
            {
                m_database.Save(order);

                return PerformErpActionSafe(order, (e, ord) => e.MarkOrderPaid(ord),
                    synced =>
                        {
                            if (!OrderStatus.IsPaid(synced.OrderStatusId))
                            {
                                throw new InvalidOperationException($" Byl odeslan pozadavek na nastaveni platby objednavky, ale objednavka ma stale stav '{synced.ErpStatusId} - {synced.ErpStatusName}', ktery Elsa mapuje na stav '{synced.OrderStatus?.Name}'");
                            }
                        });
            }
            
            order.OrderStatusId = OrderStatus.ReadyToPack.Id;
            m_database.Save(order);
            
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

                foreach (var item in order.Items)
                {
                    var assignments = item.AssignedBatches.ToList();
                    if (assignments.Count == 0)
                    {
                        throw new InvalidOperationException($"Batch assignment missing - OrderItemId = {item.Id}");
                    }

                    var sum = assignments.Sum(a => a.Quantity);
                    if (Math.Abs(sum - item.Quantity) > 0.0001m)
                    {
                        throw new InvalidOperationException($"Invalid assignment quantity - OrderItemId={item.Id}, diff={Math.Abs(sum - item.Quantity)}");
                    }

                    foreach (var assignment in assignments)
                    {
                        m_batchFacade.AssignOrderItemToBatch(assignment.MaterialBatchId, order, item.Id, assignment.Quantity);
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
                            });
                }

                tx.Commit();

                return order;
            }
        }

        public IEnumerable<IPurchaseOrder> GetAndSyncPaidOrders(DateTime historyDepth, bool skipErp = false)
        {
            m_log.Info($"Nacitam zaplacene objednavky od {historyDepth}");

            if (skipErp)
            {
                foreach (var rpo in m_orderRepository.GetOrdersByStatus(OrderStatus.ReadyToPack))
                {
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
                    yield return m_orderRepository.GetOrder(importedId);
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

        public IEnumerable<IPurchaseOrder> GetOrdersByUsedBatch(int batchId, int pageSize, int pageNumber)
        {
            return m_database.SelectFrom<IPurchaseOrder>()
                .Join(o => o.Items)
                .Join(o => o.Items.Each().KitChildren)
                .Join(o => o.Items.Each().AssignedBatches)
                .Join(o => o.Items.Each().KitChildren.Each().AssignedBatches)
                .Where(o => o.ProjectId == m_session.Project.Id)
                .Where(
                    o =>
                        (o.Items.Each().AssignedBatches.Each().MaterialBatchId == batchId) ||
                        (o.Items.Each().KitChildren.Each().AssignedBatches.Each().MaterialBatchId == batchId))
                .OrderBy(o => o.OrderNumber)
                .Skip(pageSize*pageNumber)
                .Take(pageSize)
                .Execute();
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
            Action<IPurchaseOrder> validateSyncedOrder)
        {
            m_database.Save(order);

            var orderId = order.Id;

            if (order.ErpId == null)
            {
                throw new InvalidOperationException("Cannot perform ERP operation for order without ERP");
            }

            var erp = m_clientFactory.GetErpClient(order.ErpId.Value);

            erpAction(erp, order);

            var erpOrder = erp.LoadOrder(order.OrderNumber);
            if (erpOrder == null)
            {
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
                throw new InvalidOperationException($"Chyba pri pokusu o zpracovani objednavky '{order.OrderNumber}' po pozadavku na zmenu v systemu '{order.Erp?.Description}': {ex.Message}");
            }

            return order;
        }
    }
}
