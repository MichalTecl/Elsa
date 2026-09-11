using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Model;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Integration;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class PurchaseOrderRepository : IPurchaseOrderRepository
    {
        private readonly IErpClientFactory _erpClientFactory;
        private readonly IDatabase _database;
        private readonly ISession _session;
        private readonly IDictionary<int, IErpDataMapper> _mapperIndex = new Dictionary<int, IErpDataMapper>();
        private readonly IProductRepository _productRepository;

        private readonly ICurrencyRepository _currencyRepository;
        private readonly IOrderStatusMappingRepository _statusMappingRepository;
        private readonly List<IPurchaseOrder> _ordersCache = new List<IPurchaseOrder>();
        private readonly ICache _cache;
        private readonly ILog _log;
        private readonly IOrderImportFailureRepository _orderImportFailureRepository;

        public PurchaseOrderRepository(IErpClientFactory erpClientFactory, IDatabase database, ISession session, ICurrencyRepository currencyRepository, IOrderStatusMappingRepository statusMappingRepository, IProductRepository productRepository, ICache cache, ILog log, IOrderImportFailureRepository orderImportFailureRepository)
        {
            _erpClientFactory = erpClientFactory;
            _database = database;
            _session = session;
            _currencyRepository = currencyRepository;
            _statusMappingRepository = statusMappingRepository;
            _productRepository = productRepository;
            _cache = cache;
            _log = log;
            _orderImportFailureRepository = orderImportFailureRepository;
        }

        public long ImportErpOrder(IErpOrderModel orderModel)
        {
            _log.Info($"Starting import of order {orderModel.OrderNumber}");

            long result;
            using (var trx = _database.OpenTransaction())
            {
                var mapper = GetMapper(orderModel);

                var host = new OrderMapperHost(mapper, orderModel, this, _database, _currencyRepository, _statusMappingRepository, _productRepository);
                if (!host.Map())
                {
                    _log.Info($"Order {orderModel.OrderNumber} unchanged (orderId={host.Order.Id}) - done");
                    trx.Commit();
                    return host.Order.Id;
                }

                _currencyRepository.SaveCurrency(host.Currency);
                host.Order.CurrencyId = host.Currency.Id;

                if (host.DeliveryAddress != null)
                {
                    _database.Save(host.DeliveryAddress);
                    host.Order.DeliveryAddressId = host.DeliveryAddress.Id;
                }
                else
                {
                    host.Order.DeliveryAddressId = null;
                }

                if (host.InvoiceAddress != null)
                {
                    _database.Save(host.InvoiceAddress);
                    host.Order.InvoiceAddressId = host.InvoiceAddress.Id;
                }
                else
                {
                    host.Order.InvoiceAddressId = null;
                }

                host.Order.ErpId = orderModel.ErpSystemId;

                if (host.Order.Id < 1)
                {
                    _log.Info($"Order {orderModel.OrderNumber} will be INSERTed");
                    host.Order.InsertUserId = _session.User.Id;
                    host.Order.InsertDt = DateTime.Now;
                }
                else
                {
                    _log.Info($"Order {orderModel.OrderNumber} will be UPDATEd (orderId={host.Order.Id})");
                }

                host.Order.ProjectId = _session.Project.Id;

                _database.Save(host.Order);

                foreach (var item in host.Items)
                {
                    var isInsert = item.Id < 1;

                    item.PurchaseOrderId = host.Order.Id;
                    _database.Save(item);

                    _log.Info($"OrderItem OrderId={host.Order?.Id} OrderNr={host.Order?.OrderNumber} ItemId={item.Id} {(isInsert ? "inserted" : "updated")}");
                }

                foreach(var priceElement in host.PriceElements)
                {
                    priceElement.PurchaseOrderId = host.Order.Id;
                    _database.Save(priceElement);
                }

                foreach (var delId in host.OrderItemsToDelete)
                {
                    _log.Info($"Existing orderItem OrderId={host.Order?.Id} OrderNr={host.Order?.OrderNumber} ItemId={delId} will be deleted");

                    var kitChildren = _database.SelectFrom<IOrderItem>().Where(i => i.KitParentId == delId).Execute()
                        .ToList();

                    var orderItemIds = new List<long>(1 + kitChildren.Count);
                    orderItemIds.Add(delId);
                    orderItemIds.AddRange(kitChildren.Select(ch => ch.Id));

                    var oimbs = _database.SelectFrom<IOrderItemMaterialBatch>().Where(ob => ob.OrderItemId.InCsv(orderItemIds)).Execute().ToList();
                    if (oimbs.Any())
                    {
                        _database.DeleteAll(oimbs);
                    }

                    if (kitChildren.Any())
                    {
                        _database.DeleteAll(kitChildren);
                    }

                    var item = _database.SelectFrom<IOrderItem>().Where(i => i.Id == delId).Execute().FirstOrDefault();
                    if (item != null)
                    {
                        _database.Delete(item);
                    }
                }

                foreach(var pelmDelId in host.PriceElementsToDelete)
                {
                    var item = _database.SelectFrom<IOrderPriceElement>().Where(i => i.Id == pelmDelId).Execute().FirstOrDefault();
                    if (item != null)
                    {
                        _database.Delete(item);
                    }
                }

                result = host.Order.Id;

                _log.Info($"Import of order {orderModel.OrderNumber} (orderId={host.Order.Id}) completed, commiting the transaction");

                trx.Commit();
            }

            return result;
        }

        public void ImportErpOrders(int erpId, List<IErpOrderModel> orders)
        {
            _log.Info($"Got {orders.Count} orders to sync");

            if (orders.Count == 0)
            {
                _log.Info("Skipping saving...");
                return;
            }

            var erp = _erpClientFactory.GetErpClient(erpId);
            var candidates = new List<OrderImportCandidate>(orders.Count);
            var failedOrdersCount = 0;

            foreach (var order in orders)
            {
                try
                {
                    if (order == null)
                        throw new ArgumentNullException(nameof(order));

                    if (order.ErpSystemId != erpId)
                        throw new Exception($"Passed order for another ERP (expected={erpId}, actual={order.ErpSystemId})");

                    var purchaseDate = erp.Mapper.GetPurchaseDate(order);
                    if ((DateTime.Now - purchaseDate).TotalDays > 400)
                    {
                        _log.Error($"ERP exported order {GetOrderIdentification(order)} which has purchase date older than 400 days ({purchaseDate}). Removing from import.");
                        continue;
                    }

                    candidates.Add(new OrderImportCandidate(order, purchaseDate));
                }
                catch (Exception ex)
                {
                    failedOrdersCount++;
                    HandleOrderImportFailure(erpId, order, "initial validation", ex);
                }
            }

            if (candidates.Count == 0)
            {
                _log.Info($"No valid orders to save; failed orders: {failedOrdersCount}");
                return;
            }

            var loadExistingFrom = candidates.Min(c => c.PurchaseDate);
            var loadExistingTo = candidates.Max(c => c.PurchaseDate);

            _log.Info($"Loading existing orders index from {loadExistingFrom} to {loadExistingTo}");

            var existingOrders = _database.SelectFrom<IPurchaseOrder>()
                .Where(o => o.ProjectId == _session.Project.Id)
                .Where(o => o.ErpId == erpId)
                .Where(o => o.PurchaseDate >= loadExistingFrom)
                .OrderByDesc(o => o.Id)
                .Execute()
                .ToList();

            var existingOrdersIndex = new Dictionary<string, IPurchaseOrder>(existingOrders.Count);

            foreach ( var order in existingOrders)
            {
                if (existingOrdersIndex.ContainsKey(order.OrderNumber))
                {
                    _log.Error($"Duplicity order number found in the database: {order.OrderNumber}");
                    continue;
                }

                existingOrdersIndex[order.OrderNumber] = order;
            }

            _log.Info($"Loaded {existingOrdersIndex.Count} existing records");

            var dirtyOrders = new List<OrderImportCandidate>(candidates.Count);

            foreach (var candidate in candidates)
            {
                try
                {
                    var order = candidate.Order;
                    if (!existingOrdersIndex.TryGetValue(order.OrderNumber, out var existingOrder))
                    {
                        _log.Info($"Order {order.OrderNumber} is new - adding to import");
                        dirtyOrders.Add(candidate);
                        continue;
                    }

                    if (existingOrder.OrderHash != order.OrderHash)
                    {
                        _log.Info($"Order {order.OrderNumber} is changed - adding to import");
                        dirtyOrders.Add(candidate);
                    }
                    else
                    {
                        _orderImportFailureRepository.Resolve(erpId, order.OrderNumber);
                    }
                }
                catch (Exception ex)
                {
                    failedOrdersCount++;
                    HandleOrderImportFailure(erpId, candidate.Order, "change detection", ex);
                }
            }

            _log.Info($"{dirtyOrders.Count} orders considered to be new or changed");

            if(dirtyOrders.Count == 0)
            {
                _log.Info("Skipping orders saving");
                return;
            }

            var preloadFrom = dirtyOrders.Min(c => c.PurchaseDate);
            var preloadTo = dirtyOrders.Max(c => c.PurchaseDate);

            PreloadOrders(preloadFrom, preloadTo);

            foreach (var candidate in dirtyOrders)
            {
                try
                {
                    ImportErpOrder(candidate.Order);
                    _orderImportFailureRepository.Resolve(erpId, candidate.Order.OrderNumber);
                }
                catch (Exception ex)
                {
                    failedOrdersCount++;
                    HandleOrderImportFailure(erpId, candidate.Order, "database save", ex);
                }
            }

            _log.Info($"Orders import completed; received: {orders.Count}, failed: {failedOrdersCount}");
        }

        private void HandleOrderImportFailure(int erpId, IErpOrderModel order, string phase, Exception exception)
        {
            var orderNumber = order?.OrderNumber;
            if (!string.IsNullOrWhiteSpace(orderNumber))
                _orderImportFailureRepository.RegisterFailure(erpId, orderNumber, exception.ToString());

            _log.Error($"Import of order {GetOrderIdentification(order)} failed during {phase}. The order will be skipped and the import will continue.", exception);
        }

        private static string GetOrderIdentification(IErpOrderModel order)
        {
            if (order == null)
                return "<null>";

            try
            {
                return order.OrderNumber ?? order.ErpOrderId ?? "<unknown>";
            }
            catch
            {
                return "<unavailable>";
            }
        }

        private sealed class OrderImportCandidate
        {
            public OrderImportCandidate(IErpOrderModel order, DateTime purchaseDate)
            {
                Order = order;
                PurchaseDate = purchaseDate;
            }

            public IErpOrderModel Order { get; }

            public DateTime PurchaseDate { get; }
        }

        public IPurchaseOrder TryLoadOrderByOrderNumber(string orderNumber)
        {
            var cachedOrder =
                _ordersCache.FirstOrDefault(i => (i.ProjectId == _session.Project.Id) && (i.OrderNumber == orderNumber));
            if (cachedOrder != null)
            {
                return cachedOrder;
            }

            var result = BuildOrdersQuery()
                    .Where(o => o.OrderNumber == orderNumber)
                    .Execute()
                    .FirstOrDefault();

            return result;
        }

        public void PreloadOrders(DateTime from, DateTime to)
        {
            _ordersCache.Clear();

            _log.Info($"Preloading orders cache {from} - {to}");

            _ordersCache.AddRange(BuildOrdersQuery().Where(o => (o.PurchaseDate >= @from) && (o.PurchaseDate <= to)).Execute());

            _log.Info($"Orders cache loaded {_ordersCache.Count} orders");
        }

        public IEnumerable<OrdersOverviewModel> GetOrdersOverview(DateTime from, DateTime to)
        {
            const string sql = @"SELECT erp.Description as Erp, po.OrderStatusId StatusId, COUNT(po.Id) Count
                                     FROM PurchaseOrder po
                                     LEFT JOIN Erp     erp ON (po.ErpId = erp.Id)
                                    WHERE po.PurchaseDate >= @from
                                      AND po.PurchaseDate <= @to
                                      AND po.ProjectId = @projectId
                                    GROUP BY erp.Description, po.OrderStatusId
                                    ORDER BY po.OrderStatusId, erp.Description; ";

            return
                _database.Sql()
                    .Execute(sql)
                    .WithParam("@from", from)
                    .WithParam("@to", to)
                    .WithParam("@projectId", _session.Project.Id)
                    .MapRows(row => new OrdersOverviewModel()
                                        {
                                            ErpName = row.GetString(0),
                                            StatusId = row.GetInt32(1),
                                            Count = row.GetInt32(2)
                                        });
        }

        public IEnumerable<IPurchaseOrder> GetOrdersByStatus(IOrderStatus status, DateTime from, DateTime to)
        {
            return
                BuildOrdersQuery()
                    .Where(o => o.PurchaseDate >= from)
                    .Where(o => o.PurchaseDate <= to)
                    .Where(o => o.OrderStatusId == status.Id)
                    .Execute();
        }

        public int CountOrdersToPack()
        {
            return _database.Sql()
                .ExecuteWithParams("SELECT COUNT(Id) FROM PurchaseOrder WHERE ProjectId={0} AND OrderStatusId = {1}",
                    _session.Project.Id,
                    OrderStatus.ReadyToPack.Id)
                .Scalar<int>();
        }

        public IEnumerable<IPurchaseOrder> GetOrdersByStatus(IOrderStatus status)
        {
            return GetOrdersByStatus(status, DateTime.Now.AddYears(-50), DateTime.Now.AddDays(1));
        }

        public IEnumerable<IPurchaseOrder> GetOrders(Action<IQueryBuilder<IPurchaseOrder>> query)
        {
            var q = BuildOrdersQuery();
            query(q);

            return q.Execute();
        }

        public IPurchaseOrder GetOrder(long orderId)
        {
            return BuildOrdersQuery().Where(o => o.Id == orderId).Execute().FirstOrDefault();
        }

        public IEnumerable<IPurchaseOrder> GetOrdersToMarkPaidInErp()
        {
            var orderIds = _database.Sql().Execute(@"SELECT DISTINCT po.Id
                                          FROM PurchaseOrder po
                                    INNER JOIN ErpOrderStatusMapping mp ON (po.ErpStatusId = mp.ErpStatusId
                                                                        AND po.ErpId = mp.ErpId)
                                    WHERE po.PaymentPairingDt IS NULL AND mp.SetPaidInErp = 1 AND po.ProjectId = @projectId").WithParam("@projectId", _session.Project.Id).MapRows(r => r.GetInt64(0));

            foreach (var orderId in orderIds)
            {
                yield return GetOrder(orderId);
            }
        }

        public int GetMissingPaymentsCount(int businessDaysTolerance)
        {
            const string sql = @"SELECT COUNT(Id)
                                  FROM PurchaseOrder po
                                 WHERE po.IsPayOnDelivery = 0
                                   AND po.OrderStatusId = 2
                                   AND po.ProjectId = {0}
                                ";

            return _database.Sql().ExecuteWithParams(sql, _session.Project.Id).Scalar<int?>() ?? 0;
        }

        public IEnumerable<IOrderItem> GetChildItemsByParentItemId(long parentItemId)
        {
            return
                _database.SelectFrom<IOrderItem>()
                    .Join(i => i.KitParent)
                    .Join(i => i.KitParent.PurchaseOrder)
                    .Join(i => i.AssignedBatches)
                    //.Where(i => i.KitParentId != null)
                    .Where(i => i.KitParentId == parentItemId)
                    .Where(i => i.KitParent.PurchaseOrder.ProjectId == _session.Project.Id)
                    .Execute();
        }

        public IEnumerable<IPurchaseOrder> GetOrdersByMaterialBatch(int batchId)
        {
            return _database.SelectFrom<IPurchaseOrder>()
                    .Join(po => po.Items)
                    .Join(po => po.Items.Each().AssignedBatches.Each().MaterialBatch)
                    .Join(po => po.Items.Each().KitChildren.Each().AssignedBatches.Each().MaterialBatch)
                    .Where(po => po.ProjectId == _session.Project.Id)
                    .Where(
                        po =>
                            (po.Items.Each().AssignedBatches.Each().MaterialBatchId == batchId)
                            || (po.Items.Each().KitChildren.Each().AssignedBatches.Each().MaterialBatchId == batchId))
                    .Execute();
        }

        public void UpdateOrderItemBatch(IOrderItem orderItem, int batchId, decimal quantity)
        {
            if (quantity > orderItem.Quantity)
            {
                throw new InvalidOperationException($"Ze šarže nemůže být odebráno množství větší, než je množství objednané položky");
            }

            using (var tx = _database.OpenTransaction())
            {
                var existingAssignments =
                    _database.SelectFrom<IOrderItemMaterialBatch>()
                        .Where(a => a.OrderItemId == orderItem.Id)
                        .Execute()
                        .ToList();

                /*
                var assignmentsToRemove = existingAssignments.Where(a => a.MaterialBatchId == batchId);
                _database.DeleteAll(assignmentsToRemove);
                */

                var alreadyAllocatedAmount =
                    existingAssignments.Where(a => a.MaterialBatchId != batchId).Sum(a => a.Quantity);

                if ((alreadyAllocatedAmount + quantity) > orderItem.Quantity)
                {
                    throw new InvalidOperationException($"Položka již má přiřazené šarže. Výsledné přiřazení by překračovalo celkové množství položky.");
                }

                var assignment = _database.New<IOrderItemMaterialBatch>();
                assignment.MaterialBatchId = batchId;
                assignment.OrderItemId = orderItem.Id;
                assignment.Quantity = quantity;
                assignment.AssignmentDt = DateTime.Now;
                assignment.UserId = _session.User.Id;

                _database.Save(assignment);

                tx.Commit();
            }
        }

        public IEnumerable<IPurchaseOrder> GetReturns(int month, int year)
        {
            var ids = new List<long>();

            _database.Sql().ExecuteWithParams(
                "SELECT Id FROM PurchaseOrder WHERE OrderStatusId=6 AND ProjectId = {0} AND MONTH(ReturnDt) = {1} AND YEAR(ReturnDt) = {2}",
                _session.Project.Id,
                month,
                year).ReadRows<long>(ids.Add);

            foreach (var id in ids)
            {
                yield return GetOrder(id);
            }
        }

        public long? SearchOrder(string orderNumberEndsWith, int orderStatusId)
        {
            var x = _database.Sql().ExecuteWithParams(
                "SELECT TOP 2 Id FROM PurchaseOrder po WHERE po.ProjectId = {0} AND po.OrderStatusId = {1} AND po.OrderNumber LIKE {2}",
                _session.Project.Id,
                orderStatusId,
                $"%{orderNumberEndsWith}"
            ).MapRows(reader => reader.GetInt64(0));

            if (x.Count > 1)
            {
                throw new ArgumentException($"Objednávku nelze jednoznačně určit, použijte celé číslo objednávky.");
            }

            return x.SingleOrDefault();
        }

        private IQueryBuilder<IPurchaseOrder> BuildOrdersQuery()
        {
            return
                _database.SelectFrom<IPurchaseOrder>()
                    .Join(o => o.DeliveryAddress)
                    .Join(o => o.InvoiceAddress)
                    .Join(o => o.Currency)
                    .Join(o => o.InsertUser)
                    .Join(o => o.Erp)
                    .Join(o => o.Items)
                    .Join(o => o.OrderStatus)
                    .Join(o => o.Items.Each().Product)
                    .Join(o => o.Items.Each().AssignedBatches)
                    .Join(o => o.Items.Each().AssignedBatches.Each().MaterialBatch)
                    .Join(o => o.Items.Each().AssignedBatches.Each())
                    .Join(o => o.Items.Each().KitChildren)
                    .Join(o => o.Items.Each().KitChildren.Each().AssignedBatches)
                    .Join(o => o.Items.Each().KitChildren.Each().AssignedBatches.Each().MaterialBatch)
                    .Join(o => o.Payment)
                    .Join(o => o.PriceElements)
                    .Where(o => o.ProjectId == _session.Project.Id);
        }

        private IErpDataMapper GetMapper(IErpOrderModel model)
        {
            IErpDataMapper mapper;
            if (!_mapperIndex.TryGetValue(model.ErpSystemId, out mapper))
            {
                mapper = _erpClientFactory.GetErpClient(model.ErpSystemId)?.Mapper;
                if (mapper == null)
                {
                    throw new InvalidOperationException($"Cannot find DataMapper for ErpSystem Id={model.ErpSystemId}");
                }

                _mapperIndex.Add(model.ErpSystemId, mapper);
            }

             return mapper;
        }

        public void SetProcessBlock(IPurchaseOrder order, string stage, string message)
        {
            var record = _database.New<IOrderProcessingBlocker>();

            record.PurchaseOrderId = order.Id;
            record.CreateDt = DateTime.Now;
            record.Message = message;
            record.DisabledStageSymbol = stage;
            record.AuthorId = _session.User.Id;

            _database.Save(record);

            _cache.Remove($"OrderProcessingBlockers_{order.Id}");
        }

        public string TryGetProcessBlockMessage(long orderId, string stage)
        {
            var blocks = _cache.ReadThrough(
                $"OrderProcessingBlockers_{orderId}",
                TimeSpan.FromMinutes(10),
                () => _database.SelectFrom<IOrderProcessingBlocker>().Where(b => b.PurchaseOrderId == orderId).Execute().ToList());

            return blocks.FirstOrDefault(b => b.DisabledStageSymbol.Equals(stage, StringComparison.InvariantCultureIgnoreCase))?.Message;
        }

        public DateTime? GetLastSuccessSyncDt(int erpId)
        {
            return _database.SelectFrom<IOrdersSyncHistory>()
                .Where(h => h.ErpId == erpId)
                .Where(h => h.EndDt != null)
                .OrderByDesc(h => h.StartDt)
                .Take(1)
                .Execute()
                .FirstOrDefault()?
                .StartDt;
        }

        public int StartSyncSession(int erpId)
        {
            var s = _database.New<IOrdersSyncHistory>();
            s.ErpId = erpId;
            s.StartDt = DateTime.Now;
            _database.Save(s);

            return s.Id;
        }

        public void EndSyncSession(int sessionId)
        {
            var record = _database.SelectFrom<IOrdersSyncHistory>()
                .Where(h => h.Id == sessionId)
                .Execute()
                .FirstOrDefault() ?? throw new ArgumentException($"OrdersSyncSession id={sessionId} does not exist");

            if (record.EndDt != null)
                throw new ArgumentException($"OrdersSyncSession id={sessionId} already ended");

            record.EndDt = DateTime.Now;

            _database.Save(record);
        }
    }
}
