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
using Elsa.Common.Interfaces;
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
            if (!material.AutomaticBatches && string.IsNullOrWhiteSpace(batchNumber?.Trim()))
            {
                throw new InvalidOperationException("Je třeba zadat číslo šarže");
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

            var batches = new List<IMaterialBatch>();
            if (!string.IsNullOrWhiteSpace(batchNumber))
            {
                batches.AddRange(m_batchRepository.GetBatches(new BatchKey(material.Id, batchNumber)).OrderBy(b => b.Created));
            }
            else
            {
                batches.AddRange(m_batchRepository
                    .GetMaterialBatches(DateTime.Now.AddYears(-2), DateTime.Now.AddYears(99), true, materialId, false,
                        false, false).Select(b => b.Batch).OrderBy(b => b.Created));
            }
            
            var eventAmount = new Amount(quantity, unit);
            var groupingKey = Guid.NewGuid().ToString("N");
            var eventDt = DateTime.Now;

            using (var tx = m_cache.OpenTransaction())
            {
                foreach (var batch in batches)
                {
                    var available = m_batchFacade.Value.GetAvailableAmount(batch.Id);

                    if (available.IsNotPositive)
                    {
                        continue;
                    }

                    var toProcess = m_amountProcessor.Min(available, eventAmount);
                    
                    var evt = m_cache.New<IMaterialStockEvent>();
                    evt.EventGroupingKey = groupingKey;
                    evt.BatchId = batch.Id;
                    evt.Delta = toProcess.Value;
                    evt.UnitId = toProcess.Unit.Id;
                    evt.Note = reason;
                    evt.TypeId = eventTypeId;
                    evt.UserId = m_session.User.Id;
                    evt.EventDt = eventDt;
                    evt.SourcePurchaseOrderId = sourceOrderId;
                    m_cache.Save(evt);

                    m_batchFacade.Value.ReleaseBatchAmountCache(batch.Id);

                    eventAmount = m_amountProcessor.Subtract(eventAmount, toProcess);

                    if (!eventAmount.IsPositive)
                    {
                        break;
                    }
                }

                tx.Commit();
            }
        }
        
        public IEnumerable<IMaterialStockEvent> GetBatchEvents(BatchKey key)
        {
            var batchNumber = key.GetBatchNumber(m_batchRepository);
            var materialId = key.GetMaterialId(m_batchRepository);

            return
                m_database.SelectFrom<IMaterialStockEvent>()
                    .Join(e => e.Batch)
                    .Where(m => m.ProjectId == m_session.Project.Id)
                    .Where(m => m.Batch.MaterialId == materialId && m.Batch.BatchNumber == batchNumber)
                    .Execute().Select(b => new MaterialStockEventAdapter(m_serviceLocator, b));
        }

        public IEnumerable<IMaterialStockEvent> GetEvents(DateTime @from, DateTime to, int inventoryId)
        {
            return m_database.SelectFrom<IMaterialStockEvent>()
                .Join(e => e.Batch)
                .Join(e => e.Batch.Material)
                .Where(e => e.ProjectId == m_session.Project.Id)
                .Where(e => e.Batch.Material.InventoryId == inventoryId)
                .Where(e => e.EventDt >= from && e.EventDt < to).Execute()
                .Select(b => new MaterialStockEventAdapter(m_serviceLocator, b));
        }

        public void DeleteStockEvent(int eventId, bool allEventsInGroup)
        {
            using (var tx = m_database.OpenTransaction())
            {
                var evt =
                    m_database.SelectFrom<IMaterialStockEvent>()
                        .Where(e => e.Id == eventId)
                        .Where(e => e.ProjectId == m_session.Project.Id)
                        .Take(1)
                        .Execute()
                        .FirstOrDefault()
                        .Ensure();

                var events = new List<IMaterialStockEvent>();

                if (allEventsInGroup)
                {
                    events.AddRange(m_database.SelectFrom<IMaterialStockEvent>()
                        .Where(e => e.ProjectId == m_session.Project.Id)
                        .Where(e => e.EventGroupingKey == evt.EventGroupingKey).Execute());
                }
                else
                {
                    events.Add(evt);
                }

                foreach (var e in events)
                {
                    m_batchFacade.Value.ReleaseBatchAmountCache(
                        m_batchRepository.GetBatchById(e.BatchId).Ensure().Batch);
                    m_database.Delete(e);
                }

                tx.Commit();
            }
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

                foreach (var assignment in assignments)
                {
                    m_database.Delete(assignment);
                    m_batchFacade.Value.ReleaseBatchAmountCache(assignment.MaterialBatchId);
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
                .Where(e => e.EventDt >= from && e.EventDt < to).Execute()
                .Select(b => new MaterialStockEventAdapter(m_serviceLocator, b));
        }
    }
}
