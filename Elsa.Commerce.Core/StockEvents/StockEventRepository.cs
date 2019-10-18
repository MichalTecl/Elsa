using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Commerce.Core.Adapters;
using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Accounting.InvoiceFormItemBridges;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Robowire;
using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.StockEvents
{
    public class StockEventRepository : IStockEventRepository
    {
        private readonly IPerProjectDbCache m_cache;
        private readonly Lazy<IMaterialBatchFacade> m_batchFacade;
        private readonly IMaterialBatchRepository m_batchRepository;
        private readonly AmountProcessor m_amountProcessor;
        private readonly IMaterialRepository m_materialRepository;
        private readonly ISession m_session;
        private readonly IDatabase m_database;
        private readonly IUnitRepository m_unitRepository;
        private readonly IUnitConversionHelper m_conversionHelper;
        private readonly IPurchaseOrderRepository m_orderRepository;
        private readonly IServiceLocator m_serviceLocator;

        public StockEventRepository(IPerProjectDbCache cache,
            Lazy<IMaterialBatchFacade> batchFacade,
            IMaterialBatchRepository batchRepository,
            AmountProcessor amountProcessor,
            IMaterialRepository materialRepository,
            ISession session,
            IDatabase database, IUnitRepository unitRepository, IUnitConversionHelper conversionHelper,
            IPurchaseOrderRepository orderRepository, IServiceLocator serviceLocator)
        {
            m_cache = cache;
            m_batchFacade = batchFacade;
            m_batchRepository = batchRepository;
            m_amountProcessor = amountProcessor;
            m_materialRepository = materialRepository;
            m_session = session;
            m_database = database;
            m_unitRepository = unitRepository;
            m_conversionHelper = conversionHelper;
            m_orderRepository = orderRepository;
            m_serviceLocator = serviceLocator;
        }

        public IEnumerable<IStockEventType> GetAllEventTypes()
        {
            return m_cache.ReadThrough("stockEventTypes", q => q.SelectFrom<IStockEventType>().OrderBy(t => t.Name));
        }

        public void SaveEvent(int eventTypeId, int materialId, string batchNumber, decimal quantity, string reason,
            string unitSymbol, long? sourceOrderId = null)
        {
            var eventType = GetAllEventTypes().FirstOrDefault(et => et.Id == eventTypeId).Ensure();

            if (!eventType.IsSubtracting)
            {
                throw new NotSupportedException("Toto jeste neni");
            }
             
            if (eventType.RequiresNote && ((reason?.Trim() ?? string.Empty).Length < 5))
            {
                throw new InvalidOperationException("Důvod musí mít alespoň 5 znaků");
            }
            
            var material = m_materialRepository.GetMaterialById(materialId).Ensure();

            int? batchId = null;
            if (!material.AutomaticBatches && string.IsNullOrWhiteSpace(batchNumber?.Trim()))
            {
                throw new InvalidOperationException("Je třeba zadat číslo šarže");
            }

            if (!string.IsNullOrWhiteSpace(batchNumber))
            {
                batchId = m_batchRepository.GetBatchIdByNumber(materialId, batchNumber);
                if (batchId == null)
                {
                    throw new InvalidOperationException("Neplatné číslo šarže");
                }
            }

            var unit = material.NominalUnit;
            if (!string.IsNullOrWhiteSpace(unitSymbol))
            {
                unit = m_unitRepository.GetUnitBySymbol(unitSymbol);
                if (unit == null || (!m_conversionHelper.AreCompatible(material.NominalUnit.Id, unit.Id)))
                {
                    throw new InvalidOperationException($"Pro materiál \"{material.Name}\" nelze použít jednotku \"{unitSymbol}\"");
                }
            }

            var eventAmount = new Amount(quantity, unit);
           
            using (var tx = m_cache.OpenTransaction())
            {
                var resolutions = new List<Tuple<IMaterialBatch, Amount>>();
                
                resolutions.AddRange(m_batchFacade.Value.AutoResolve(materialId, eventAmount, true, batchId));

                if (resolutions.Any(r => r.Item1 == null))
                {
                    var max = m_amountProcessor.Sum(resolutions.Where(r => r.Item1 != null).Select(r => r.Item2));
                    throw new InvalidOperationException($"Není možné odebrat více než {max}");
                }

                var groupingKey = Guid.NewGuid().ToString("N");
                foreach (var resolution in resolutions)
                {
                    var evt = m_cache.New<IMaterialStockEvent>();
                    evt.EventGroupingKey = groupingKey;
                    evt.BatchId = resolution.Item1.Id;
                    evt.Delta = resolution.Item2.Value;
                    evt.UnitId = resolution.Item2.Unit.Id;
                    evt.Note = reason;
                    evt.TypeId = eventTypeId;
                    evt.UserId = m_session.User.Id;
                    evt.EventDt = DateTime.Now;
                    evt.SourcePurchaseOrderId = sourceOrderId;
                    m_cache.Save(evt);

                    m_batchFacade.Value.ReleaseBatchAmountCache(resolution.Item1);
                }

                tx.Commit();
            }
        }

        public IEnumerable<IMaterialStockEvent> GetBatchEvents(int batchId)
        {
            return
                m_database.SelectFrom<IMaterialStockEvent>()
                    .Where(m => m.ProjectId == m_session.Project.Id)
                    .Where(m => m.BatchId == batchId)
                    .Execute().Select(b => new MaterialStockEventAdapter(m_serviceLocator, b));
        }

        public IEnumerable<IMaterialStockEvent> GetEvents(DateTime @from, DateTime to, int inventoryId)
        {
            return m_database.SelectFrom<IMaterialStockEvent>()
                .Join(e => e.Batch)
                .Join(e => e.Batch.Material)
                .Where(e => e.ProjectId == m_session.Project.Id)
                .Where(e => e.Batch.Material.InventoryId == inventoryId)
                .Where(e => e.EventDt >= from && e.EventDt <= to).Execute()
                .Select(b => new MaterialStockEventAdapter(m_serviceLocator, b));
        }

        public void DeleteStockEvent(int eventId)
        {
            var evt =
                m_database.SelectFrom<IMaterialStockEvent>()
                    .Where(e => e.Id == eventId)
                    .Where(e => e.ProjectId == m_session.Project.Id)
                    .Take(1)
                    .Execute()
                    .FirstOrDefault()
                    .Ensure();

            m_batchFacade.Value.ReleaseBatchAmountCache(m_batchRepository.GetBatchById(evt.BatchId).Ensure().Batch);

            m_database.Delete(evt);
        }

        public void MoveOrderToEvent(long returnedOrderId, int eventTypeId, string reason)
        {
            using (var tx = m_database.OpenTransaction())
            {
                var order = m_orderRepository.GetOrder(returnedOrderId).Ensure();
                var assignments = new List<IOrderItemMaterialBatch>();

                foreach (var orderitem in order.Items)
                {
                    assignments.AddRange(orderitem.AssignedBatches);

                    foreach (var kitItem in orderitem.KitChildren)
                    {
                        assignments.AddRange(kitItem.AssignedBatches);
                    }
                }

                var invoiceFormBridges = new List<IOrderItemInvoiceFormItem>();
                foreach (var orderItemMaterialBatch in assignments)
                {
                    invoiceFormBridges.AddRange(m_database.SelectFrom<IOrderItemInvoiceFormItem>().Where(i => i.BatchAssignmentId == orderItemMaterialBatch.Id).Execute());
                }

                m_database.DeleteAll(invoiceFormBridges);
                m_database.DeleteAll(assignments);

                foreach (var assignment in assignments)
                {
                    m_batchFacade.Value.InvalidateBatchCache(assignment.MaterialBatchId);
                }

                foreach (var assignment in assignments)
                {
                    SaveEvent(eventTypeId, assignment.MaterialBatch.MaterialId, assignment.MaterialBatch.BatchNumber, assignment.Quantity, reason, null, returnedOrderId);
                }

                tx.Commit();
            }
        }

        public IEnumerable<IMaterialStockEvent> GetEvents(DateTime @from, DateTime to, long sourcePurchaseOrderId)
        {
            return m_database.SelectFrom<IMaterialStockEvent>()
                .Join(e => e.Type)
                .Where(e => e.ProjectId == m_session.Project.Id)
                .Where(e => e.SourcePurchaseOrderId == sourcePurchaseOrderId)
                .Where(e => e.EventDt >= from && e.EventDt <= to).Execute()
                .Select(b => new MaterialStockEventAdapter(m_serviceLocator, b));
        }
    }
}
