using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.App.MaterialLevels.Entities;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Inventory;
using Robowire.RobOrm.Core;

namespace Elsa.App.MaterialLevels.Components
{
    public class InventoryWatchRepository : IInventoryWatchRepository
    {
        private readonly IDatabase m_database;
        private readonly ICache m_cache;
        private readonly ISession m_session;
        private readonly IMaterialRepository m_materialRepository;

        public InventoryWatchRepository(ICache cache, IDatabase database, ISession session,
            IMaterialRepository materialRepository)
        {
            m_cache = cache;
            m_database = database;
            m_session = session;
            m_materialRepository = materialRepository;
        }

        public List<IMaterialInventory> GetWatchedInventories()
        {
            return m_cache.ReadThrough(GetWatchedCacheKey(), TimeSpan.FromMinutes(10), () =>
                {
                    return m_database.SelectFrom<IUserWatchedInventory>().Join(w => w.Inventory)
                        .Where(w => w.UserId == m_session.User.Id)
                        .Where(w => w.Inventory.ProjectId == m_session.Project.Id)
                        .Execute()
                        .Select(w => w.Inventory)
                        .ToList();
                });
        }

        public void WatchInventory(int inventoryId)
        {
            try
            {
                using (var tx = m_database.OpenTransaction())
                {
                    var watch = GetWatch(inventoryId);
                    if (watch == null)
                    {
                        watch = m_database.New<IUserWatchedInventory>();
                        watch.InventoryId = inventoryId;
                        watch.UserId = m_session.User.Id;

                        m_database.Save(watch);
                    }

                    tx.Commit();
                }
            }
            finally
            {
                m_cache.Remove(GetWatchesCacheKeys());
            }
        }

        public void UnwatchInventory(int inventoryId)
        {
            try
            {
                using (var tx = m_database.OpenTransaction())
                {
                    var watch = GetWatch(inventoryId);
                    if (watch != null)
                    {
                        m_database.Delete(watch);
                    }

                    tx.Commit();
                }
            }
            finally
            {

                m_cache.Remove(GetWatchesCacheKeys());
            }
        }

        public List<IMaterialInventory> GetUnwatchedInventories()
        {
            return m_cache.ReadThrough(GetUnwatchedCacheKey(), TimeSpan.FromMinutes(10), () =>
            {
                var allInventories = m_materialRepository.GetMaterialInventories().ToList();

                var result = new List<IMaterialInventory>(allInventories.Count);

                var watched = GetWatchedInventories();

                foreach (var inv in allInventories)
                {
                    if (watched.All(i => i.Id != inv.Id))
                    {
                        result.Add(inv);
                    }
                }

                return result;
            });
        }

        private IUserWatchedInventory GetWatch(int inventoryId)
        {
            return m_database.SelectFrom<IUserWatchedInventory>().Join(w => w.Inventory)
                .Where(w => w.UserId == m_session.User.Id)
                .Where(w => w.InventoryId == inventoryId)
                .Where(w => w.Inventory.ProjectId == m_session.Project.Id)
                .Take(1)
                .Execute()
                .FirstOrDefault();
        }

        private IEnumerable<string> GetWatchesCacheKeys()
        {
            yield return GetWatchedCacheKey();
            yield return GetUnwatchedCacheKey();
            yield return $"matLevelInventories_{m_session.Project.Id}";
        }

        private string GetWatchedCacheKey()
        {
            return $"inventoryWatch_{m_session.Project.Id}:{m_session.User.Id}";
        }

        private string GetUnwatchedCacheKey()
        {
            return $"inventoryUnwatch_{m_session.Project.Id}:{m_session.User.Id}";
        }
    }
}
