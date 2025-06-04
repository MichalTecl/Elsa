using Elsa.Commerce.Core.Configuration;
using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Integration;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Smtp.Core;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.Impl
{
    public class OrdersFacade : IOrdersFacade
    {
        private readonly IPurchaseOrderRepository _orderRepository;
        private readonly IDatabase _database;
        private readonly IErpClientFactory _erpClientFactory;
        private readonly ISession _session;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILog _log;
        private readonly IMaterialBatchFacade _batchFacade;
        private readonly IKitProductRepository _kitProductRepository;
        private readonly IMailSender _mailSender;
        private readonly ICache _cache;
        private readonly IAdHocOrdersSyncProvider _adhocOrdersSyncProvider;
        private readonly OrdersSystemConfig _packingConfig;

        public OrdersFacade(
            IPurchaseOrderRepository orderRepository,
            IDatabase database,
            IErpClientFactory clientFactory,
            ISession session,
            IPaymentRepository paymentRepository,
            ILog log,
            IMaterialBatchFacade batchFacade, IKitProductRepository kitProductRepository, IMailSender mailSender, ICache cache, IAdHocOrdersSyncProvider adhocOrdersSyncProvider, OrdersSystemConfig packingConfig)
        {
            _orderRepository = orderRepository;
            _database = database;
            _erpClientFactory = clientFactory;
            _session = session;
            _paymentRepository = paymentRepository;
            _log = log;
            _batchFacade = batchFacade;
            _kitProductRepository = kitProductRepository;
            _mailSender = mailSender;
            _cache = cache;
            _adhocOrdersSyncProvider = adhocOrdersSyncProvider;
            _packingConfig = packingConfig;
        }

        public IPurchaseOrder SetOrderPaid(long orderId, long? paymentId)
        {
            var order = _orderRepository.GetOrder(orderId);
            if (order == null)
            {
                throw new InvalidOperationException("Order not found");
            }

            if (paymentId != null)
            {
                var payment = _paymentRepository.GetPayment(paymentId.Value);
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

            using (var tx = _database.OpenTransaction())
            {
                _log.Info("Starting transaction for payment pairing orderId={orderId}, paymentId={paymentId}");
                if (order.ErpId != null)
                {
                    order.PaymentId = paymentId;
                    order.PaymentPairingUserId = _session.User.Id;
                    order.PaymentPairingDt = DateTime.Now;
                    _database.Save(order);

                    _log.Info($"Order saved to the database in transaction; paymentId={order.PaymentId}, pairngUserId={order.PaymentPairingUserId}, pairingDt={order.PaymentPairingDt}");
                }
                else
                {
                    _log.Error("Order.ErpId == null");
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
                    _log.Info($"Order sync skipped - statusId={order.OrderStatusId}");
                }

                tx.Commit();
                _log.Info("Transaction commited");
            }

            return order;
        }

        public IPurchaseOrder SetOrderSent(long orderId)
        {
            var order = _orderRepository.GetOrder(orderId);
            if (order == null)
            {
                throw new InvalidOperationException("Order not found");
            }

            using (var tx = _database.OpenTransaction())
            {
                order.PackingDt = DateTime.Now;
                order.PackingUserId = _session.User.Id;

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
                    _database.Save(order);
                }
                else
                {
                    order = PerformErpActionSafe(
                        order,
                        (e, o) => e.MakeOrderSent(o, warn => throw new Exception(warn)),
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
            var order = _orderRepository.GetOrder(orderId);
            if (order == null)
            {
                throw new InvalidOperationException("Order not found");
            }

            using (var tx = _database.OpenTransaction())
            {
                order.PackingDt = DateTime.Now;
                order.PackingUserId = _session.User.Id;

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
                    _database.Save(order);

                    _log.Info($"Creating OrderProcessingBlocker {OrderProcessingStageNames.BatchesAssignment} for order {order.Id} {order.OrderNumber}");

                    _orderRepository.SetProcessBlock(order, OrderProcessingStageNames.BatchesAssignment, $"Byl zahájen proces finalizace objednávky");
                }
                else
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            order = PerformErpActionSafe(
                                order,
                                (e, o) => e.MakeOrderSent(o, warn =>
                                {
                                    SendPackingMail(o,
                                        $"Objednávku {order.OrderNumber} {order.CustomerName} je třeba překontrolovat",
                                        $"V průběhu dokončování objednávky {order.OrderNumber} {order.CustomerName} nastala chyba: \"{warn}\". \r\nTato chyba nemusí bránit vyřízení objednávky, je ale třeba zkontrolovat ve Floxu, že je vše v pořádku (vydána faktura, faktura odeslána, stav objednávky...)");
                                }),
                                synced =>
                                {
                                    if (synced.OrderStatusId != OrderStatus.Sent.Id)
                                    {
                                        throw new InvalidOperationException(
                                            $" Byl odeslan pozadavek na dokonceni objednavky, ale objednavka ma stale stav '{synced.ErpStatusId} - {synced.ErpStatusName}', ktery Elsa mapuje na stav '{synced.OrderStatus?.Name}'");
                                    }
                                }, "SetOrderSent");
                        }
                        catch (Exception ex)
                        {
                            _log.Error($"Chyba pri posilani zabalene objednavky", ex);
                            SendPackingMail(order, $"Chyba odesílání objednávky {order.OrderNumber} {order.CustomerName}",
                                $"Pozor - při dokončení balení objednávky {order.OrderNumber} {order.CustomerName} nastala chyba: '{ex.Message}'\r\nZkontrolujte objednávku ručně.");

                            _orderRepository.SetProcessBlock(order, OrderProcessingStageNames.Packing, "Předchozí pokus o zabalení této objednávky selhal - je třeba ji odbavit v systému Flox");
                        }
                    });
                }

                tx.Commit();
            }
        }

        public IEnumerable<IPurchaseOrder> GetAndSyncPaidOrders(string shipProvider, bool skipErp = false)
        {
            _log.Info($"Nacitam zaplacene objednavky");

            if (!skipErp)
            {
                try
                {
                    _adhocOrdersSyncProvider.SyncPaidOrders();
                }
                catch (Exception ex)
                {
                    _log.Error($"AdHoc paid orders sync failed", ex);
                }
            }

            foreach (var rpo in _orderRepository.GetOrdersByStatus(OrderStatus.ReadyToPack))
            {
                if (!MatchShipmentProvider(rpo, shipProvider))
                    continue;

                yield return rpo;
            }

            _log.Info("Hotovo");
        }

        public IEnumerable<IOrderItem> GetAllConcreteOrderItems(IPurchaseOrder order)
        {
            foreach (var item in order.Items)
            {
                var kitItems = _kitProductRepository.GetKitForOrderItem(order, item).ToList();

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

            _database.Sql().Call("GetOrderIdsByUsedBatch").WithParam("@projectId", _session.Project.Id)
                .WithParam("@materialId", bqatch.GetMaterialId(_batchFacade))
                .WithParam("@batchNumber", bqatch.GetBatchNumber(_batchFacade))
                .WithParam("@skip", pageSize * pageNumber).WithParam("@take", pageSize).ReadRows<long, int, string, decimal>(
                    (orderId, prio, orderNum, qty) =>
                    {
                        orderIds.Add(new Tuple<long, decimal>(orderId, qty));
                    });

            var entities = _database.SelectFrom<IPurchaseOrder>()
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
                var kit = _kitProductRepository.GetKitForOrderItem(entity, orderItem).OrderBy(k => k.KitItemIndex).ToList();
                if (!kit.Any())
                {
                    continue;
                }

                foreach (var kitItem in kit)
                {
                    if (kitItem.SelectedItem != null)
                    {
                        // already selected
                        continue;
                    }

                    var groupItems = kitItem.GroupItems.ToList();
                    if (groupItems.Count == 1)
                    {
                        // we can assign this one because there is only one option
                        _kitProductRepository.SetKitItemSelection(entity, orderItem, groupItems.Single().Id, kitItem.KitItemIndex);
                        modified = true;
                        continue;
                    }


                    // let's see whether we have kit info in the customer note... (here I rely on good caching on the repo side, so it's called over and over again)
                    var parsedKit = _kitProductRepository.ParseKitNotes(entity.Id);
                    var kitSelection = parsedKit.FirstOrDefault(
                        parsed => parsed.KitDefinitionId == kitItem.KitDefinitionId
                                && parsed.KitNr == (kitItem.KitItemIndex + 1)
                                && parsed.SelectionGroupId == kitItem.GroupId
                                );
                    if (kitSelection != null)
                    {
                        var groupItem = groupItems.FirstOrDefault(gi => gi.Id == kitSelection.SelectionGroupItemId);
                        if (groupItem != null)
                        {
                            _kitProductRepository.SetKitItemSelection(entity, orderItem, groupItem.Id, kitItem.KitItemIndex);
                            modified = true;
                        }
                    }
                }
            }

            if (!modified)
            {
                return entity;
            }

            return _orderRepository.GetOrder(entity.Id);
        }

        private IPurchaseOrder PerformErpActionSafe(
            IPurchaseOrder order,
            Action<IErpClient, IPurchaseOrder> erpAction,
            Action<IPurchaseOrder> validateSyncedOrder,
            string actionLogDescription)
        {
            _log.Info($"PerformErpActionSafe started: orderId={order.Id} orderNr={order.OrderNumber} action={actionLogDescription} statusId={order.OrderStatusId} erpStatus={order.ErpStatusName}");

            _database.Save(order);

            var orderId = order.Id;

            if (order.ErpId == null || order.ErpId == 0)
            {
                throw new InvalidOperationException("Cannot perform ERP operation for order without ERP");
            }

            var erp = _erpClientFactory.GetErpClient(order.ErpId.Value);

            try
            {
                erpAction(erp, order);
                _log.Info($"PerformErpActionSafe - erpAction performed ok: orderId={order.Id} orderNr={order.OrderNumber} action={actionLogDescription}");
            }
            catch (Exception ex)
            {
                _log.Error($"PerformErpActionSafe - erpAction failed: orderId={order.Id} orderNr={order.OrderNumber} action={actionLogDescription}", ex);
                throw;
            }

            _log.Info($"Downloading order from ERP by orderNumber={order.OrderNumber}");

            var erpOrder = erp.LoadOrder(order.OrderNumber);
            if (erpOrder == null)
            {
                _log.Error($"Downloading order from ERP by orderNumber={order.OrderNumber} failed!");

                throw new InvalidOperationException(
                            $"Nepodarilo se stahnout objednavku '{order.OrderNumber}' ze systemu '{order.Erp?.Description}'");
            }

            var importedOrderId = _orderRepository.ImportErpOrder(erpOrder);
            if (importedOrderId != orderId)
            {
                throw new InvalidOperationException(
                            $"Chyba synchronizace objednavky '{order.OrderNumber}' ze systemu '{order.Erp?.Description}'");
            }

            order = _orderRepository.GetOrder(importedOrderId);

            try
            {
                validateSyncedOrder(order);
            }
            catch (Exception ex)
            {
                _log.Error($"Chyba pri pokusu o zpracovani objednavky '{order.OrderNumber}' po pozadavku na zmenu v systemu '{order.Erp?.Description}': {ex.Message}", ex);
                throw new InvalidOperationException($"Chyba pri pokusu o zpracovani objednavky '{order.OrderNumber}' po pozadavku na zmenu v systemu '{order.Erp?.Description}': {ex.Message}");
            }

            _log.Info($"PerformErpActionSafe finished ok; order reloaded from database: orderId={order.Id} orderNr={order.OrderNumber} action={actionLogDescription} statusId={order.OrderStatusId} erpStatus={order.ErpStatusName}");

            return order;
        }

        private IEnumerable<IOrderItemMaterialBatch> GetAssignedBatches(long orderItemId)
        {
            var qry = _database.SelectFrom<IOrderItemMaterialBatch>()
                .Join(ib => ib.MaterialBatch)
                .Where(ib => ib.OrderItemId == orderItemId);

            return qry.Execute();
        }

        private bool MatchShipmentProvider(IPurchaseOrder order, string shipmentProviderName)
        {
            if (shipmentProviderName == null)
                return true;

            var lookup = _cache.ReadThrough($"shipmentProviderLookup_{_session.Project.Id}", TimeSpan.FromSeconds(10), () =>
            {
                return _database.SelectFrom<IShipmentProviderLookup>().Where(p => p.ProjectId == _session.Project.Id).Execute().ToList();
            });

            var matching = lookup.Where(lkp => StringUtil.MatchStarWildcard(lkp.ShipMethodWildcardPattern, order.ShippingMethodName))
                .Select(l => l.ProviderName)
                .Distinct()
                .ToList();

            if (matching.Count != 1)
                throw new Exception($"U objednávky {order.OrderNumber} nelze určit dopravce. ShippingMethodName=\"{order.ShippingMethodName}\"; Nalezení dopravci:[{string.Join(",", matching)}]");

            return string.Equals(matching[0], shipmentProviderName, StringComparison.InvariantCultureIgnoreCase);
        }

        public IPurchaseOrder EnsureActualizedOrder(IPurchaseOrder order)
        {
            _log.Info($"Ensuring that order is actualised - Id={order.Id}, OrderNr={order.OrderNumber} ErpLastchange={(order.ErpLastChange?.ToString() ?? "null")}");

            var localLastChange = order.ErpLastChange ?? _cache.ReadThrough(
                    $"LastOrdersSyncDt_{_session.Project.Id}", 
                    TimeSpan.FromMinutes(1),
                    () => _orderRepository.GetLastSuccessSyncDt(order.ErpId.Ensure($"Order {order.Id} {order.OrderNumber} ErpId == null")) ?? DateTime.MinValue
                );

            _log.Info($"localLastchange is considered to be {localLastChange}");

            try
            {
                var erp = _erpClientFactory.GetErpClient(order.ErpId.Value);
                var erpLastChange = erp.ObtainOrderLastChange(order.OrderNumber);

                _log.Info($"order {order.OrderNumber}: localLastChange={localLastChange}, erpLastChange={erpLastChange}");

                if (localLastChange >= erpLastChange)
                {
                    _log.Info($"Local version of order {order.OrderNumber} is actual");
                    return order;
                }
                
                _log.Info($"Local version of order {order.OrderNumber} is outdated");

                var erpOrder = erp.LoadOrder(order.OrderNumber);
                var impId = _orderRepository.ImportErpOrder(erpOrder);

                if (impId != order.Id)
                {
                    _log.Error($"This is weird - updated order got another id {order.Id} x {impId}");
                }

                return _orderRepository.GetOrder(impId);
            }
            catch (Exception ex)
            {
                _log.Error($"Getting order from ERP failed: {ex.Message}", ex);
                return order;
            }
        }

        private void SendPackingMail(IPurchaseOrder order, string subject, string body)
        {
            _log.Info($"Sending order packing related mail: {subject}");

            var receivers = new List<string>();

            if (!string.IsNullOrWhiteSpace(order.PackingUser?.EMail))
                receivers.Add(order.PackingUser.EMail);
            else 
                receivers.Add(_session.User.EMail);

            if (_packingConfig.PackingFailureAdminMails != null)
                receivers.AddRange(_packingConfig.PackingFailureAdminMails);

            foreach(var receiver in receivers.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct())
            {
                _log.Info($"Sending packing mail to {receiver}");

                _mailSender.Send(receiver
                    , subject
                    , body);
            }            
        }
    }
}
