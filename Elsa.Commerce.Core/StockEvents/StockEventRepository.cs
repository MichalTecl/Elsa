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
using RobOrm.Core;

namespace Elsa.Commerce.Core.StockEvents
{
    public class StockEventRepository : IStockEventRepository
    {
        private readonly IPerProjectDbCache _cache;
        private readonly Lazy<IMaterialBatchFacade> _batchFacade;
        private readonly IMaterialBatchRepository _batchRepository;
        private readonly AmountProcessor _amountProcessor;
        private readonly IMaterialRepository _materialRepository;
        private readonly ISession _session;
        private readonly IDatabase _database;
        private readonly IUnitRepository _unitRepository;
        private readonly IUnitConversionHelper _conversionHelper;
        private readonly IPurchaseOrderRepository _orderRepository;
        private readonly IServiceLocator _serviceLocator;

        public StockEventRepository(IPerProjectDbCache cache,
            Lazy<IMaterialBatchFacade> batchFacade,
            IMaterialBatchRepository batchRepository,
            AmountProcessor amountProcessor,
            IMaterialRepository materialRepository,
            ISession session,
            IDatabase database, IUnitRepository unitRepository, IUnitConversionHelper conversionHelper,
            IPurchaseOrderRepository orderRepository, IServiceLocator serviceLocator)
        {
            _cache = cache;
            _batchFacade = batchFacade;
            _batchRepository = batchRepository;
            _amountProcessor = amountProcessor;
            _materialRepository = materialRepository;
            _session = session;
            _database = database;
            _unitRepository = unitRepository;
            _conversionHelper = conversionHelper;
            _orderRepository = orderRepository;
            _serviceLocator = serviceLocator;
        }

        public IEnumerable<IStockEventType> GetAllEventTypes()
        {
            return _cache.ReadThrough("stockEventTypes", q => q.SelectFrom<IStockEventType>().OrderBy(t => t.Name));
        }

        public void SaveEvent(int eventTypeId, int materialId, string batchNumber, decimal quantity, string reason,
            string unitSymbol, long? sourceOrderId = null)
        {
            reason = reason == null ? null : reason.Trim();

            var eventType = GetAllEventTypes().FirstOrDefault(et => et.Id == eventTypeId).Ensure();

            if (!eventType.IsSubtracting)
            {
                throw new NotSupportedException("Toto jeste neni");
            }
             
            if (eventType.RequiresNote && ((reason?.Trim() ?? string.Empty).Length < 5))
            {
                throw new InvalidOperationException("Důvod musí mít alespoň 5 znaků");
            }
            
            var material = _materialRepository.GetMaterialById(materialId).Ensure();
            if (!material.AutomaticBatches && string.IsNullOrWhiteSpace(batchNumber?.Trim()))
            {
                throw new InvalidOperationException("Je třeba zadat číslo šarže");
            }

            var unit = material.NominalUnit;
            if (!string.IsNullOrWhiteSpace(unitSymbol))
            {
                unit = _unitRepository.GetUnitBySymbol(unitSymbol);
                if (unit == null || (!_conversionHelper.AreCompatible(material.NominalUnit.Id, unit.Id)))
                {
                    throw new InvalidOperationException($"Pro materiál \"{material.Name}\" nelze použít jednotku \"{unitSymbol}\"");
                }
            }

            var batches = new List<IMaterialBatch>();
            if (!string.IsNullOrWhiteSpace(batchNumber))
            {
                batches.AddRange(_batchRepository.GetBatches(new BatchKey(material.Id, batchNumber)).OrderBy(b => b.Created));
            }
            else
            {
                batches.AddRange(_batchRepository
                    .GetMaterialBatches(DateTime.Now.AddYears(-2), DateTime.Now.AddYears(99), true, materialId, false,
                        false, false).Select(b => b.Batch).OrderBy(b => b.Created));
            }
            
            var eventAmount = new Amount(quantity, unit);
            var groupingKey = Guid.NewGuid().ToString("N");
            var eventDt = DateTime.Now;

            using (var tx = _cache.OpenTransaction())
            {
                foreach (var batch in batches)
                {
                    var available = _batchFacade.Value.GetAvailableAmount(batch.Id);

                    if (available.IsNotPositive)
                    {
                        continue;
                    }

                    var toProcess = _amountProcessor.Min(available, eventAmount);
                    
                    var evt = _cache.New<IMaterialStockEvent>();
                    evt.EventGroupingKey = groupingKey;
                    evt.BatchId = batch.Id;
                    evt.Delta = toProcess.Value;
                    evt.UnitId = toProcess.Unit.Id;
                    evt.Note = reason;
                    evt.TypeId = eventTypeId;
                    evt.UserId = _session.User.Id;
                    evt.EventDt = eventDt;
                    evt.SourcePurchaseOrderId = sourceOrderId;
                    _cache.Save(evt);

                    _batchFacade.Value.ReleaseBatchAmountCache(batch.Id);

                    eventAmount = _amountProcessor.Subtract(eventAmount, toProcess);

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
            var batchNumber = key.GetBatchNumber(_batchRepository);
            var materialId = key.GetMaterialId(_batchRepository);

            return
                _database.SelectFrom<IMaterialStockEvent>()
                    .Join(e => e.Batch)
                    .Where(m => m.ProjectId == _session.Project.Id)
                    .Where(m => m.Batch.MaterialId == materialId && m.Batch.BatchNumber == batchNumber)
                    .Execute().Select(b => new MaterialStockEventAdapter(_serviceLocator, b));
        }

        public IEnumerable<IMaterialStockEvent> GetEvents(DateTime @from, DateTime to, int inventoryId)
        {
            return _database.SelectFrom<IMaterialStockEvent>()
                .Join(e => e.Batch)
                .Join(e => e.Batch.Material)
                .Where(e => e.ProjectId == _session.Project.Id)
                .Where(e => e.Batch.Material.InventoryId == inventoryId)
                .Where(e => e.EventDt >= from && e.EventDt < to).Execute()
                .Select(b => new MaterialStockEventAdapter(_serviceLocator, b));
        }

        public void DeleteStockEvent(int eventId, bool allEventsInGroup)
        {
            using (var tx = _database.OpenTransaction())
            {
                var evt =
                    _database.SelectFrom<IMaterialStockEvent>()
                        .Where(e => e.Id == eventId)
                        .Where(e => e.ProjectId == _session.Project.Id)
                        .Take(1)
                        .Execute()
                        .FirstOrDefault()
                        .Ensure();

                var events = new List<IMaterialStockEvent>();

                if (allEventsInGroup)
                {
                    events.AddRange(_database.SelectFrom<IMaterialStockEvent>()
                        .Where(e => e.ProjectId == _session.Project.Id)
                        .Where(e => e.EventGroupingKey == evt.EventGroupingKey).Execute());
                }
                else
                {
                    events.Add(evt);
                }

                foreach (var e in events)
                {
                    _batchFacade.Value.ReleaseBatchAmountCache(
                        _batchRepository.GetBatchById(e.BatchId).Ensure().Batch);
                    _database.Delete(e);
                }

                tx.Commit();
            }
        }

        public void MoveOrderToEvent(long returnedOrderId, int eventTypeId, string reason)
        {
            using (var tx = _database.OpenTransaction())
            {
                var order = _orderRepository.GetOrder(returnedOrderId).Ensure();
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
                    invoiceFormBridges.AddRange(_database.SelectFrom<IOrderItemInvoiceFormItem>().Where(i => i.BatchAssignmentId == orderItemMaterialBatch.Id).Execute());
                }

                _database.DeleteAll(invoiceFormBridges);

                foreach (var assignment in assignments)
                {
                    _database.Delete(assignment);
                    _batchFacade.Value.ReleaseBatchAmountCache(assignment.MaterialBatchId);
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
            return _database.SelectFrom<IMaterialStockEvent>()
                .Join(e => e.Type)
                .Where(e => e.ProjectId == _session.Project.Id)
                .Where(e => e.SourcePurchaseOrderId == sourcePurchaseOrderId)
                .Where(e => e.EventDt >= from && e.EventDt < to).Execute()
                .Select(b => new MaterialStockEventAdapter(_serviceLocator, b));
        }
    }
}
