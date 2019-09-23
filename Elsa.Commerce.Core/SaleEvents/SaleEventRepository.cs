using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Commerce.Core.Adapters;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce.SaleEvents;
using Robowire;
using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.SaleEvents
{
    public class SaleEventRepository : ISaleEventRepository
    {
        private readonly IDatabase m_database;
        private readonly ICache m_cache;
        private readonly ISession m_session;
        private readonly IServiceLocator m_serviceLocator;
        private readonly IMaterialBatchFacade m_batchFacade;
        private readonly IMaterialRepository m_materialRepository;
        private readonly AmountProcessor m_amountProcessor;

        public SaleEventRepository(IDatabase database, ICache cache, ISession session, IServiceLocator serviceLocator, IMaterialBatchFacade batchFacade, IMaterialRepository materialRepository, AmountProcessor amountProcessor)
        {
            m_database = database;
            m_cache = cache;
            m_session = session;
            m_serviceLocator = serviceLocator;
            m_batchFacade = batchFacade;
            m_materialRepository = materialRepository;
            m_amountProcessor = amountProcessor;
        }

        public ISaleEvent GetEventById(int id)
        {
            return m_cache.ReadThrough(GetEventCacheKey(id), TimeSpan.FromDays(1), () => m_database
                .SelectFrom<ISaleEvent>().Where(e => e.ProjectId == m_session.Project.Id)
                .Where(e => e.Id == id).Execute().Select(e => new SaleEventAdapter(m_serviceLocator, e))
                .FirstOrDefault());
        }

        public IEnumerable<ISaleEventAllocation> GetAllocationsByEventId(int saleEventId)
        {
            return m_cache.ReadThrough(GetAllocationsCacheKey(saleEventId), TimeSpan.FromDays(1),
                () => m_database.SelectFrom<ISaleEventAllocation>().Where(a => a.SaleEventId == saleEventId).Execute().Select(a => new SaleEventAllocationAdapter(m_serviceLocator, a))
                    .ToList());
        }

        public IEnumerable<ISaleEvent> GetEvents(int pageNumber, int pageSize)
        {
            var events = m_database.SelectFrom<ISaleEvent>().Where(e => e.ProjectId == m_session.Project.Id).OrderByDesc(e => e.EventDt)
                .Skip(pageSize * pageNumber).Take(pageSize).Execute();

            foreach (var e in events)
            {
                yield return new SaleEventAdapter(m_serviceLocator, e);
            }
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
                using (var tx = m_database.OpenTransaction())
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
                m_cache.Remove(GetEventCacheKey(id));
                m_cache.Remove(GetAllocationsCacheKey(id));
            }

            var evt = GetEventById(id);

            foreach (var batch in evt.Allocations.Select(a => a.Batch).Distinct())
            {
                m_batchFacade.ReleaseBatchAmountCache(batch);
            }

            return evt;
        }

        private ISaleEvent CreateEvent(Action<ISaleEvent> entity)
        {
            var e = m_database.New<ISaleEvent>();
            entity(e);

            e.UserId = m_session.User.Id;
            e.ProjectId = m_session.Project.Id;
            e.EventDt = DateTime.Now;

            m_database.Save(e);

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
                m_amountProcessor.GreaterThan(dto.ReturnedQuantity, dto.AllocatedQuantity))
            {
                throw new InvalidOperationException("Nelze vracet vetsi nez blokovane mnozstvi");
            }

            if (!existingAllocations.Any())
            {
                var batches = m_batchFacade.ProposeAllocations(dto.MaterialId, dto.BatchNumber, dto.AllocatedQuantity)
                    .ToList();
                if (batches.Any(b => b.Item1 == null))
                {
                    throw new InvalidOperationException($"Pozadovane mnozstvi {dto.AllocatedQuantity} {m_materialRepository.GetMaterialById(dto.MaterialId)?.Name} neni v sarzi {dto.BatchNumber} k dispozici");
                }

                foreach (var batch in batches)
                {
                    m_database.Save(m_database.New<ISaleEventAllocation>(a =>
                    {
                        a.AllocationDt = DateTime.Now;
                        a.AllocatedQuantity = dto.AllocatedQuantity.Value;
                        a.UnitId = dto.AllocatedQuantity.Unit.Id;
                        a.BatchId = batch.Item1.Value;
                        a.AllocationUserId = m_session.User.Id;
                        a.SaleEventId = eventId;

                        if (dto.ReturnedQuantity != null)
                        {
                            a.ReturnedQuantity = dto.ReturnedQuantity.Value;
                            a.ReturnDt = DateTime.Now;
                            a.ReturnUserId = m_session.User.Id;
                        }
                    }));
                }

                return;
            }

            var totalAllocatedInDb =
                m_amountProcessor.Sum(existingAllocations.Select(a => new Amount(a.AllocatedQuantity, a.Unit)));

            if (!m_amountProcessor.AreEqual(totalAllocatedInDb, dto.AllocatedQuantity))
            {
                throw new InvalidOperationException("Nelze zmenit alokovane mnozstvi pro existujici akci");
            }

            var totalReturnedInDb = m_amountProcessor.Sum(existingAllocations.Where(a => a.ReturnedQuantity != null)
                .Select(a => new Amount(a.ReturnedQuantity.Value, a.Unit)));

            var returnedAmount = dto.ReturnedQuantity == null
                ? new Amount(0, dto.AllocatedQuantity.Unit)
                : dto.ReturnedQuantity;

            if (m_amountProcessor.AreEqual(totalReturnedInDb, returnedAmount))
            {
                return;
            }

            if (m_amountProcessor.GreaterThan(totalReturnedInDb, returnedAmount))
            {
                throw new InvalidOperationException("Nelze snizovat vracene mnozstvi");
            }

            var newReturn = m_amountProcessor.Subtract(returnedAmount, totalReturnedInDb);

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
                    m_amountProcessor.Subtract(new Amount(existingAloc.AllocatedQuantity, existingAloc.Unit),
                        alreadyReturnedHere);

                if (!toReturnHere.IsPositive)
                {
                    continue;
                }

                var actuallyReturningHere = m_amountProcessor.Min(toReturnHere, newReturn);

                existingAloc.ReturnedQuantity = actuallyReturningHere.Value;
                existingAloc.UnitId = actuallyReturningHere.Unit.Id;
                m_database.Save(existingAloc);

                newReturn = m_amountProcessor.Subtract(newReturn, actuallyReturningHere);
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
