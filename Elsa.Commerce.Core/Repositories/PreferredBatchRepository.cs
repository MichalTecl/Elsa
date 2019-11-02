using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Configuration;
using Elsa.Commerce.Core.Model;
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


        private string GetPreferredBatchesCacheKey()
        {
            return $"prf_pck_bchs_usr_{m_session.User.Id}";
        }

        public string GetPrefferedBatchNumber(int materialId)
        {
            return LoadPreferrences().FirstOrDefault(p => p.MaterialId == materialId)?.BatchNumber;
        }

        public void InvalidatePreferrenceByMaterialId(int materialId)
        {
            var existingPreferrence = LoadPreferrences().FirstOrDefault(p => p.MaterialId == materialId);
            if (existingPreferrence == null)
            {
                return;
            }

            m_database.Delete(existingPreferrence);
            m_cache.Remove(GetPreferredBatchesCacheKey());
        }

        public void SetBatchPreferrence(BatchKey batchKey)
        {
            var materialId = batchKey.GetMaterialId(m_batchRepository);
            var batchNumber = batchKey.GetBatchNumber(m_batchRepository);

            var existingPreferrence = LoadPreferrences().FirstOrDefault(p => p.MaterialId == materialId) ?? m_database.New<IPackingPreferredBatch>(
                                          p =>
                                          {
                                              p.MaterialId = materialId;
                                              p.UserId = m_session.User.Id;
                                          });

            if (existingPreferrence.BatchNumber?.Equals(batchNumber, StringComparison.InvariantCultureIgnoreCase) !=
                true)
            {
                m_cache.Remove(GetPreferredBatchesCacheKey());
            }
            else if ((DateTime.Now - existingPreferrence.LastActivity).TotalMinutes < 5)
            {
                //to reduce db writes
                return;
            }

            existingPreferrence.BatchNumber = batchNumber;
            existingPreferrence.LastActivity = DateTime.Now;

            m_database.Save(existingPreferrence);
        }

        private IEnumerable<IPackingPreferredBatch> LoadPreferrences()
        {
            return m_cache.ReadThrough(GetPreferredBatchesCacheKey(), TimeSpan.FromHours(1),  () =>
            {
                var rawDb = m_database.SelectFrom<IPackingPreferredBatch>().Where(p => p.UserId == m_session.User.Id)
                    .Execute().ToList();

                for (var i = rawDb.Count - 1; i >= 0; i--)
                {
                    var item = rawDb[i];

                    if ((DateTime.Now - item.LastActivity).TotalHours > m_config.BatchPreferrenceLifetimeHours)
                    {
                        m_database.Delete(item);
                        rawDb.RemoveAt(i);
                    }
                }

                return rawDb;
            });
        }
    }
}
