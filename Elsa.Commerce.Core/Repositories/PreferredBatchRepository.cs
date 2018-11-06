using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Configuration;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class PreferredBatchRepository : IPackingPreferredBatchRepository
    {
        private readonly ISession m_session;
        private readonly IDatabase m_database;
        private readonly ICache m_cache;
        private readonly PackingPreferredBatchesConfig m_config;
        private readonly IMaterialBatchRepository m_batchRepository;

        public PreferredBatchRepository(ISession session, IDatabase database, ICache cache, PackingPreferredBatchesConfig config, IMaterialBatchRepository batchRepository)
        {
            m_session = session;
            m_database = database;
            m_cache = cache;
            m_config = config;
            m_batchRepository = batchRepository;
        }

        public IEnumerable<IPackingPreferredBatch> GetPreferredBatches()
        {
            while (true)
            {
                var prefs =
                    m_cache.ReadThrough(
                        GetPreferredBatchesCacheKey(),
                        TimeSpan.FromHours(1),
                        () =>
                            m_database.SelectFrom<IPackingPreferredBatch>()
                                .Where(b => b.UserId == m_session.User.Id)
                                .Execute()).ToList();

                var now = DateTime.Now;
                var toInvalidate = prefs.Where(p => (now - p.LastActivity).TotalHours > m_config.BatchPreferrenceLifetimeHours).ToList();
                if (toInvalidate.Count == 0)
                {
                    return prefs;
                }

                m_database.DeleteAll(toInvalidate);

                m_cache.Remove(GetPreferredBatchesCacheKey());
            }
        }

        public void SetBatchPreferrence(int batchId)
        {
            var batch = m_batchRepository.GetBatchById(batchId);
            if (batch == null)
            {
                return;
            }

            var preferrences = GetPreferredBatches();

            m_cache.Remove(GetPreferredBatchesCacheKey());

            var oldPreferrence = preferrences.Where(p => p.MaterialId == batch.Batch.MaterialId);
            m_database.DeleteAll(oldPreferrence);

            var preferrence = m_database.New<IPackingPreferredBatch>(
                b =>
                    {
                        b.LastActivity = DateTime.Now;
                        b.UserId = m_session.User.Id;
                        b.MaterialId = batch.Batch.MaterialId;
                        b.BatchId = batch.Batch.Id;
                    });

            m_database.Save(preferrence);
        }

        public void NotifyPreferrenceActivity(int preferrenceId)
        {
            var preferrences = GetPreferredBatches();
            var pref = preferrences.FirstOrDefault(p => p.Id == preferrenceId);
            if (pref == null)
            {
                return;
            }

            pref.LastActivity = DateTime.Now;
            m_database.Save(pref);
        }

        public void InvalidatePreferrence(int preferrenceId)
        {
            var preferrence = GetPreferredBatches().FirstOrDefault(p => p.Id == preferrenceId);
            if (preferrence != null)
            {
                m_database.Delete(preferrence);
            }

            m_cache.Remove(GetPreferredBatchesCacheKey());
        }

        public void RemoveBatchFromPreferrence(int batchId)
        {
            var preferrence = GetPreferredBatches().FirstOrDefault(p => p.BatchId == batchId);
            if (preferrence != null)
            {
                InvalidatePreferrence(preferrence.Id);
            }
        }

        private string GetPreferredBatchesCacheKey()
        {
            return $"prf_pck_bchs_usr_{m_session.User.Id}";
        }
    }
}
