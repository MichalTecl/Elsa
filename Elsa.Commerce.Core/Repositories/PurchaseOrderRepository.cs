using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Model;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class PurchaseOrderRepository : IPurchaseOrderRepository
    {
        private readonly IErpClientFactory m_erpClientFactory;
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly IDictionary<int, IErpDataMapper> m_mapperIndex = new Dictionary<int, IErpDataMapper>();
        private readonly IProductRepository m_productRepository;

        private readonly ICurrencyRepository m_currencyRepository;
        private readonly IOrderStatusMappingRepository m_statusMappingRepository;
        private readonly List<IPurchaseOrder> m_cache = new List<IPurchaseOrder>();
        
        public PurchaseOrderRepository(IErpClientFactory erpClientFactory, IDatabase database, ISession session, ICurrencyRepository currencyRepository, IOrderStatusMappingRepository statusMappingRepository, IProductRepository productRepository)
        {
            m_erpClientFactory = erpClientFactory;
            m_database = database;
            m_session = session;
            m_currencyRepository = currencyRepository;
            m_statusMappingRepository = statusMappingRepository;
            m_productRepository = productRepository;
        }

        public long ImportErpOrder(IErpOrderModel orderModel)
        {
            long result;
            using (var trx = m_database.OpenTransaction())
            {
                var mapper = GetMapper(orderModel);

                var host = new OrderMapperHost(mapper, orderModel, this, m_database, m_currencyRepository, m_statusMappingRepository, m_productRepository);
                if (!host.Map())
                {
                    trx.Commit();
                    return host.Order.Id;
                }
                
                m_currencyRepository.SaveCurrency(host.Currency);
                host.Order.CurrencyId = host.Currency.Id;

                if (host.DeliveryAddress != null)
                {
                    m_database.Save(host.DeliveryAddress);
                    host.Order.DeliveryAddressId = host.DeliveryAddress.Id;
                }
                else
                {
                    host.Order.DeliveryAddressId = null;
                }

                if (host.InvoiceAddress != null)
                {
                    m_database.Save(host.InvoiceAddress);
                    host.Order.InvoiceAddressId = host.InvoiceAddress.Id;
                }
                else
                {
                    host.Order.InvoiceAddressId = null;
                }
                
                host.Order.ErpId = orderModel.ErpSystemId;

                if (host.Order.Id < 1)
                {
                    host.Order.InsertUserId = m_session.User.Id;
                    host.Order.InsertDt = DateTime.Now;
                }

                host.Order.ProjectId = m_session.Project.Id;

                m_database.Save(host.Order);

                foreach (var item in host.Items)
                {
                    item.PurchaseOrderId = host.Order.Id;
                    m_database.Save(item);
                }

                foreach (var delId in host.OrderItemsToDelete)
                {
                    var kitChildren = m_database.SelectFrom<IOrderItem>().Where(i => i.KitParentId == delId).Execute()
                        .ToList();

                    if (kitChildren.Any())
                    {
                        m_database.DeleteAll(kitChildren);
                    }

                    var item = m_database.SelectFrom<IOrderItem>().Where(i => i.Id == delId).Execute().FirstOrDefault();
                    if (item != null)
                    {
                        m_database.Delete(item);
                    }
                }

                result = host.Order.Id;

                trx.Commit();
            }

            return result;
        }

        public IPurchaseOrder TryLoadOrderByOrderNumber(string orderNumber)
        {
            var cachedOrder =
                m_cache.FirstOrDefault(i => (i.ProjectId == m_session.Project.Id) && (i.OrderNumber == orderNumber));
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
            m_cache.Clear();
            
            m_cache.AddRange(BuildOrdersQuery().Where(o => (o.PurchaseDate >= @from) && (o.PurchaseDate <= to)).Execute());
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
                m_database.Sql()
                    .Execute(sql)
                    .WithParam("@from", from)
                    .WithParam("@to", to)
                    .WithParam("@projectId", m_session.Project.Id)
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
            return m_database.Sql()
                .ExecuteWithParams("SELECT COUNT(Id) FROM PurchaseOrder WHERE ProjectId={0} AND OrderStatusId = {1}",
                    m_session.Project.Id, 
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
            var orderIds = m_database.Sql().Execute(@"SELECT DISTINCT po.Id
                                          FROM PurchaseOrder po
                                    INNER JOIN ErpOrderStatusMapping mp ON (po.ErpStatusId = mp.ErpStatusId 
                                                                        AND po.ErpId = mp.ErpId)
                                    WHERE po.PaymentPairingDt IS NULL AND mp.SetPaidInErp = 1 AND po.ProjectId = @projectId").WithParam("@projectId", m_session.Project.Id).MapRows(r => r.GetInt64(0));

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

            return m_database.Sql().ExecuteWithParams(sql, m_session.Project.Id).Scalar<int?>() ?? 0;
        }

        public IEnumerable<IOrderItem> GetChildItemsByParentItemId(long parentItemId)
        {
            return
                m_database.SelectFrom<IOrderItem>()
                    .Join(i => i.KitParent)
                    .Join(i => i.KitParent.PurchaseOrder)
                    .Join(i => i.AssignedBatches)
                    //.Where(i => i.KitParentId != null)
                    .Where(i => i.KitParentId == parentItemId)
                    .Where(i => i.KitParent.PurchaseOrder.ProjectId == m_session.Project.Id)
                    .Execute();
        }

        public IEnumerable<IPurchaseOrder> GetOrdersByMaterialBatch(int batchId)
        {
            return m_database.SelectFrom<IPurchaseOrder>()
                    .Join(po => po.Items)
                    .Join(po => po.Items.Each().AssignedBatches.Each().MaterialBatch)
                    .Join(po => po.Items.Each().KitChildren.Each().AssignedBatches.Each().MaterialBatch)
                    .Where(po => po.ProjectId == m_session.Project.Id)
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

            using (var tx = m_database.OpenTransaction())
            {
                var existingAssignments =
                    m_database.SelectFrom<IOrderItemMaterialBatch>()
                        .Where(a => a.OrderItemId == orderItem.Id)
                        .Execute()
                        .ToList();

                /*
                var assignmentsToRemove = existingAssignments.Where(a => a.MaterialBatchId == batchId);
                m_database.DeleteAll(assignmentsToRemove);
                */

                var alreadyAllocatedAmount =
                    existingAssignments.Where(a => a.MaterialBatchId != batchId).Sum(a => a.Quantity);

                if ((alreadyAllocatedAmount + quantity) > orderItem.Quantity)
                {
                    throw new InvalidOperationException($"Položka již má přiřazené šarže. Výsledné přiřazení by překračovalo celkové množství položky.");
                }

                var assignment = m_database.New<IOrderItemMaterialBatch>();
                assignment.MaterialBatchId = batchId;
                assignment.OrderItemId = orderItem.Id;
                assignment.Quantity = quantity;
                assignment.AssignmentDt = DateTime.Now;
                assignment.UserId = m_session.User.Id;

                m_database.Save(assignment);

                tx.Commit();
            }
        }

        public IEnumerable<IPurchaseOrder> GetReturns(int month, int year)
        {
            var ids = new List<long>();

            m_database.Sql().ExecuteWithParams(
                "SELECT Id FROM PurchaseOrder WHERE OrderStatusId=6 AND ProjectId = {0} AND MONTH(ReturnDt) = {1} AND YEAR(ReturnDt) = {2}",
                m_session.Project.Id,
                month,
                year).ReadRows<long>(ids.Add);

            foreach (var id in ids)
            {
                yield return GetOrder(id);
            }
        }

        public long? SearchOrder(string orderNumberEndsWith, int orderStatusId)
        {
            var x = m_database.Sql().ExecuteWithParams(
                "SELECT TOP 2 Id FROM PurchaseOrder po WHERE po.ProjectId = {0} AND po.OrderStatusId = {1} AND po.OrderNumber LIKE {2}",
                m_session.Project.Id,
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
                m_database.SelectFrom<IPurchaseOrder>()
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
                    .Where(o => o.ProjectId == m_session.Project.Id);
        }
        
        private IErpDataMapper GetMapper(IErpOrderModel model)
        {
            IErpDataMapper mapper;
            if (!m_mapperIndex.TryGetValue(model.ErpSystemId, out mapper))
            {
                mapper = m_erpClientFactory.GetErpClient(model.ErpSystemId)?.Mapper;
                if (mapper == null)
                {
                    throw new InvalidOperationException($"Cannot find DataMapper for ErpSystem Id={model.ErpSystemId}");
                }

                m_mapperIndex.Add(model.ErpSystemId, mapper);
            }

             return mapper;
        }
    }
}
