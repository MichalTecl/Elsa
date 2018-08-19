﻿using System;

using Elsa.Common;
using Elsa.Core.Entities.Commerce.Commerce;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Impl
{
    public class OrdersFacade : IOrdersFacade
    {
        private readonly IPurchaseOrderRepository m_orderRepository;
        private readonly IDatabase m_database;
        private readonly IErpClientFactory m_clientFactory;
        private readonly ISession m_session;

        public OrdersFacade(IPurchaseOrderRepository orderRepository, IDatabase database, IErpClientFactory clientFactory, ISession session)
        {
            m_orderRepository = orderRepository;
            m_database = database;
            m_clientFactory = clientFactory;
            m_session = session;
        }

        public IPurchaseOrder SetOrderPaid(long orderId, long? paymentId)
        {
            using (var tx = m_database.OpenTransaction())
            {
                var order = m_orderRepository.GetOrder(orderId);
                if (order == null)
                {
                    throw new InvalidOperationException($"Order not found");
                }

                order.PaymentId = paymentId;
                order.PaymentPairingUserId = m_session.User.Id;
                order.PaymentPairingDt = DateTime.Now;

                if (order.ErpId != null)
                {
                    m_database.Save(order);

                    var erp = m_clientFactory.GetErpClient(order.ErpId.Value);
                    erp.MarkOrderPaid(order.OrderNumber);

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
                    if (!OrderStatus.IsPaid(order.OrderStatusId))
                    {
                        throw new InvalidOperationException(
                                  $"Chyba pri pokusu o zpracovani objednavky '{order.OrderNumber}'. Do systemu '{order.Erp?.Description}' byl odeslan pozadavek na nastaveni platby objednavky, ale objednavka ma stale stav '{order.ErpStatusId} - {order.ErpStatusName}', ktery Elsa mapuje na stav '{order.OrderStatus?.Name}'");
                    }
                }
                else
                {
                    order.OrderStatusId = OrderStatus.ReadyToPack.Id;
                    m_database.Save(order);
                }

                tx.Commit();

                return order;
            }
        }
    }
}
