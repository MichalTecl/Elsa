using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Commerce.Core.Adapters;
using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce.SaleEvents;
using Robowire;
using RobOrm.Core;

namespace Elsa.Commerce.Core.SaleEvents
{
    public class SaleEventRepository : ISaleEventRepository
    {
        private readonly IDatabase _database;
        private readonly ICache _cache;
        private readonly ISession _session;
        private readonly IServiceLocator _serviceLocator;
        private readonly IMaterialBatchFacade _batchFacade;
        private readonly IMaterialRepository _materialRepository;
        private readonly AmountProcessor _amountProcessor;

        public SaleEventRepository(IDatabase database, ICache cache, ISession session, IServiceLocator serviceLocator, IMaterialBatchFacade batchFacade, IMaterialRepository materialRepository, AmountProcessor amountProcessor)
        {
            _database = database;
            _cache = cache;
            _session = session;
            _serviceLocator = serviceLocator;
            _batchFacade = batchFacade;
            _materialRepository = materialRepository;
            _amountProcessor = amountProcessor;
        }

        public ISaleEvent GetEventById(int id)
        {
            return _cache.ReadThrough(GetEventCacheKey(id), TimeSpan.FromDays(1), () => _database
                .SelectFrom<ISaleEvent>().Where(e => e.ProjectId == _session.Project.Id)
                .Where(e => e.Id == id).Execute().Select(e => new SaleEventAdapter(_serviceLocator, e))
                .FirstOrDefault());
        }

        public IEnumerable<ISaleEventAllocation> GetAllocationsByEventId(int saleEventId)
        {
            return _cache.ReadThrough(GetAllocationsCacheKey(saleEventId), TimeSpan.FromDays(1),
                () => _database.SelectFrom<ISaleEventAllocation>().Where(a => a.SaleEventId == saleEventId).Execute().Select(a => new SaleEventAllocationAdapter(_serviceLocator, a))
                    .ToList());
        }

        public IEnumerable<ISaleEvent> GetEvents(int pageNumber, int pageSize)
        {
            var events = _database.SelectFrom<ISaleEvent>().Where(e => e.ProjectId == _session.Project.Id).OrderByDesc(e => e.EventDt)
                .Skip(pageSize * pageNumber).Take(pageSize).Execute();

            foreach (var e in events)
            {
                yield return new SaleEventAdapter(_serviceLocator, e);
            }
        }

        public IEnumerable<ISaleEvent> GetEvents(DateTime @from, DateTime to)
        {
            return _database.SelectFrom<ISaleEvent>()
                .Where(e => e.ProjectId == _session.Project.Id)
                .Where(e => e.EventDt >= from && e.EventDt < to).Execute()
                .Select(e => new SaleEventAdapter(_serviceLocator, e));
        }

        public ISaleEvent WriteEvent(int id, Action<ISaleEvent> entity, IEnumerable<SaleEventAllocationDto> allocations)
        {
            var aloList = allocations.ToList();

            if (!aloList.Any())
            {
                throw new InvalidOperationException("Zadne polozky");
            }

            var itmKeys = new HashSet<string>();

            foreach (var alocation in aloList)
            {
                var key = $"{alocation.BatchNumber}|{alocation.MaterialId}";
                if (!itmKeys.Add(key))
                {
                    throw new InvalidOperationException($"Dokument nesmi obsahovat dvakrat stejnou sarzi ({alocation.BatchNumber})");
                }
            }

            try
            {
                using (var tx = _database.OpenTransaction())
                {
                    List<ISaleEventAllocation> existingAllocations;

                    if (id < 1)
                    {
                        id = CreateEvent(entity).Id;
                        existingAllocations = new List<ISaleEventAllocation>(0);
                    }
                    else
                    {
                        var e = GetEventById(id).Ensure("Neplatne ID akce");
                        existingAllocations = e.Allocations.ToList();
                    }

                    foreach (var allocation in aloList)
                    {
                        SaveAllocation(id, existingAllocations, allocation);
                    }

                    tx.Commit();
                }
            }
            finally
            {
                _cache.Remove(GetEventCacheKey(id));
                _cache.Remove(GetAllocationsCacheKey(id));
            }

            var evt = GetEventById(id);

            foreach (var batch in evt.Allocations.Select(a => a.Batch).Distinct())
            {
                _batchFacade.ReleaseBatchAmountCache(batch);
            }

            return evt;
        }

        public IEnumerable<ISaleEventAllocation> GetAllocationsByBatch(BatchKey key)
        {
            var batchNumber = key.GetBatchNumber(_batchFacade);
            var materialId = key.GetMaterialId(_batchFacade);

            var allocs = _database.SelectFrom<ISaleEventAllocation>()
                .Join(a => a.Batch)
                .Where(b =>    b.Batch.MaterialId == materialId 
                            && b.Batch.BatchNumber == batchNumber 
                            && b.Batch.ProjectId == _session.Project.Id)
                .Execute();

            return allocs.Select(a => new SaleEventAllocationAdapter(_serviceLocator, a));
        }

        private ISaleEvent CreateEvent(Action<ISaleEvent> entity)
        {
            var e = _database.New<ISaleEvent>();
            entity(e);

            e.UserId = _session.User.Id;
            e.ProjectId = _session.Project.Id;
            e.EventDt = DateTime.Now;

            _database.Save(e);

            return e;
        }

        private void SaveAllocation(int eventId, List<ISaleEventAllocation> existing, SaleEventAllocationDto dto)
        {
            var existingAllocations = existing
                .Where(a => a.Batch.MaterialId == dto.MaterialId && a.Batch.BatchNumber == dto.BatchNumber).ToList();

            if (existing.Any() && !existingAllocations.Any())
            {
                throw new InvalidOperationException("Pri zmene existujici prodejni akce nesmi byt zmeneny polozky, jen vracene mnozstvi");
            }

            if (dto.ReturnedQuantity != null &&
                _amountProcessor.GreaterThan(dto.ReturnedQuantity, dto.AllocatedQuantity))
            {
                throw new InvalidOperationException("Nelze vracet vetsi nez blokovane mnozstvi");
            }

            if (!existingAllocations.Any())
            {
                var batches = _batchFacade.ProposeAllocations(dto.MaterialId, dto.BatchNumber, dto.AllocatedQuantity)
                    .ToList();
                if (batches.Any(b => b.Item1 == null))
                {
                    throw new InvalidOperationException($"Pozadovane mnozstvi {dto.AllocatedQuantity} {_materialRepository.GetMaterialById(dto.MaterialId)?.Name} neni v sarzi {dto.BatchNumber} k dispozici");
                }

                foreach (var batch in batches)
                {
                    _database.Save(_database.New<ISaleEventAllocation>(a =>
                    {
                        a.AllocationDt = DateTime.Now;
                        a.AllocatedQuantity = batch.Item2.Value;
                        a.UnitId = batch.Item2.Unit.Id;
                        a.BatchId = batch.Item1.Value;
                        a.AllocationUserId = _session.User.Id;
                        a.SaleEventId = eventId;

                        if (dto.ReturnedQuantity != null)
                        {
                            a.ReturnedQuantity = dto.ReturnedQuantity.Value;
                            a.ReturnDt = DateTime.Now;
                            a.ReturnUserId = _session.User.Id;
                        }
                    }));
                }

                return;
            }

            var totalAllocatedInDb =
                _amountProcessor.Sum(existingAllocations.Select(a => new Amount(a.AllocatedQuantity, a.Unit)));

            if (!_amountProcessor.AreEqual(totalAllocatedInDb, dto.AllocatedQuantity))
            {
                throw new InvalidOperationException("Nelze zmenit alokovane mnozstvi pro existujici akci");
            }

            var totalReturnedInDb = _amountProcessor.Sum(existingAllocations.Where(a => a.ReturnedQuantity != null)
                .Select(a => new Amount(a.ReturnedQuantity.Value, a.Unit)));

            var returnedAmount = dto.ReturnedQuantity == null
                ? new Amount(0, dto.AllocatedQuantity.Unit)
                : dto.ReturnedQuantity;

            if (_amountProcessor.AreEqual(totalReturnedInDb, returnedAmount))
            {
                return;
            }

            if (_amountProcessor.GreaterThan(totalReturnedInDb, returnedAmount))
            {
                throw new InvalidOperationException("Nelze snizovat vracene mnozstvi");
            }

            var newReturn = _amountProcessor.Subtract(returnedAmount, totalReturnedInDb);

            foreach (var existingAloc in existingAllocations)
            {
                if (!newReturn.IsPositive)
                {
                    break;
                }

                var alreadyReturnedHere = existingAloc.ReturnedQuantity == null
                    ? new Amount(0, existingAloc.Unit)
                    : new Amount(existingAloc.ReturnedQuantity.Value, existingAloc.Unit);

                var toReturnHere =
                    _amountProcessor.Subtract(new Amount(existingAloc.AllocatedQuantity, existingAloc.Unit),
                        alreadyReturnedHere);

                if (!toReturnHere.IsPositive)
                {
                    continue;
                }

                var actuallyReturningHere = _amountProcessor.Min(toReturnHere, newReturn);

                existingAloc.ReturnedQuantity = actuallyReturningHere.Value;
                existingAloc.UnitId = actuallyReturningHere.Unit.Id;
                _database.Save(existingAloc);

                newReturn = _amountProcessor.Subtract(newReturn, actuallyReturningHere);
            }

            if (newReturn.IsPositive)
            {
                throw new InvalidOperationException("Nebylo mozne vratit vsechno pozadovane mnozstvi");
            }
        }

        private static string GetEventCacheKey(int eventId)
        {
            return $"saleEvt{eventId}";
        }

        private static string GetAllocationsCacheKey(int eventId)
        {
            return $"saleEvtAllo{eventId}";
        }
    }
}
