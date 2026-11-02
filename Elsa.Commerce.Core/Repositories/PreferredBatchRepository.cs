using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Configuration;
using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

using RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class PreferredBatchRepository : IPackingPreferredBatchRepository
    {
        private readonly ISession _session;
        private readonly IDatabase _database;
        private readonly ICache _cache;
        private readonly OrdersSystemConfig _config;
        private readonly IMaterialBatchRepository _batchRepository;

        public PreferredBatchRepository(ISession session, IDatabase database, ICache cache, OrdersSystemConfig config, IMaterialBatchRepository batchRepository)
        {
            _session = session;
            _database = database;
            _cache = cache;
            _config = config;
            _batchRepository = batchRepository;
        }


        private string GetPreferredBatchesCacheKey()
        {
            return $"prf_pck_bchs_usr_{_session.User.Id}";
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

            _database.Delete(existingPreferrence);
            _cache.Remove(GetPreferredBatchesCacheKey());
        }

        public void SetBatchPreferrence(BatchKey batchKey)
        {
            var materialId = batchKey.GetMaterialId(_batchRepository);
            var batchNumber = batchKey.GetBatchNumber(_batchRepository);

            var existingPreferrence = LoadPreferrences().FirstOrDefault(p => p.MaterialId == materialId) ?? _database.New<IPackingPreferredBatch>(
                                          p =>
                                          {
                                              p.MaterialId = materialId;
                                              p.UserId = _session.User.Id;
                                          });

            if (existingPreferrence.BatchNumber?.Equals(batchNumber, StringComparison.InvariantCultureIgnoreCase) !=
                true)
            {
                _cache.Remove(GetPreferredBatchesCacheKey());
            }
            else if ((DateTime.Now - existingPreferrence.LastActivity).TotalMinutes < 5)
            {
                //to reduce db writes
                return;
            }

            existingPreferrence.BatchNumber = batchNumber;
            existingPreferrence.LastActivity = DateTime.Now;

            _database.Save(existingPreferrence);
        }

        private IEnumerable<IPackingPreferredBatch> LoadPreferrences()
        {
            return _cache.ReadThrough(GetPreferredBatchesCacheKey(), TimeSpan.FromHours(1),  () =>
            {
                var rawDb = _database.SelectFrom<IPackingPreferredBatch>().Where(p => p.UserId == _session.User.Id)
                    .Execute().ToList();

                for (var i = rawDb.Count - 1; i >= 0; i--)
                {
                    var item = rawDb[i];

                    if ((DateTime.Now - item.LastActivity).TotalHours > _config.BatchPreferrenceLifetimeHours)
                    {
                        _database.Delete(item);
                        rawDb.RemoveAt(i);
                    }
                }

                return rawDb;
            });
        }
    }
}
