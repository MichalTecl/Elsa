using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.App.MaterialLevels.Components.Model;
using Elsa.App.MaterialLevels.Entities;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
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

        public List<InventoryModel> GetVisibleTabs()
        {
            return m_cache.ReadThrough(GetVisibleTabsCacheKey(), TimeSpan.FromMinutes(10), () =>
            {
                var allTabs = GetAllTabs();
                var hiddenTabs = GetHiddenTabsFromDatabase();

                return allTabs
                    .Where(tab => hiddenTabs.All(hidden => !IsSameTab(hidden, tab)))
                    .ToList();
            });
        }

        public void ShowTab(int inventoryId, string materialLevelReportingGroup)
        {
            try
            {
                using (var tx = m_database.OpenTransaction())
                {
                    var hiddenTab = GetHiddenTab(inventoryId, materialLevelReportingGroup);
                    if (hiddenTab != null)
                    {
                        m_database.Delete(hiddenTab);
                    }

                    tx.Commit();
                }
            }
            finally
            {
                m_cache.Remove(GetTabCacheKeys());
            }
        }

        public void HideTab(int inventoryId, string materialLevelReportingGroup)
        {
            try
            {
                using (var tx = m_database.OpenTransaction())
                {
                    var hiddenTab = GetHiddenTab(inventoryId, materialLevelReportingGroup);
                    if (hiddenTab == null)
                    {
                        hiddenTab = m_database.New<IUserHiddenMaterialLevelTab>();
                        hiddenTab.InventoryId = inventoryId;
                        hiddenTab.MaterialLevelReportingGroup = NormalizeGroup(materialLevelReportingGroup);
                        hiddenTab.UserId = m_session.User.Id;

                        m_database.Save(hiddenTab);
                    }

                    tx.Commit();
                }
            }
            finally
            {
                m_cache.Remove(GetTabCacheKeys());
            }
        }

        public List<InventoryModel> GetHiddenTabs()
        {
            return m_cache.ReadThrough(GetHiddenTabsCacheKey(), TimeSpan.FromMinutes(10), () =>
            {
                var allTabs = GetAllTabs();
                var hiddenTabs = GetHiddenTabsFromDatabase();

                return allTabs
                    .Where(tab => hiddenTabs.Any(hidden => IsSameTab(hidden, tab)))
                    .ToList();
            });
        }

        private List<InventoryModel> GetAllTabs()
        {
            var inventories = m_materialRepository.GetMaterialInventories()
                .OrderBy(i => i.CanBeConnectedToTag ? 0 : 1)
                .ThenBy(i => i.Name)
                .ToList();

            var materials = m_materialRepository.GetAllMaterials(null, true).ToList();
            var result = new List<InventoryModel>();

            foreach (var inventory in inventories)
            {
                result.Add(CreateTab(inventory.Id, inventory.Name, null));

                var groups = materials
                    .Where(m => m.InventoryId == inventory.Id)
                    .Select(m => NormalizeGroup(m.MaterialLevelReportingGroup))
                    .Where(g => g != null)
                    .Distinct(StringComparer.InvariantCultureIgnoreCase)
                    .OrderBy(g => g)
                    .ToList();

                foreach (var group in groups)
                {
                    result.Add(CreateTab(inventory.Id, $"{inventory.Name} - {group}", group));
                }
            }

            return result;
        }

        private InventoryModel CreateTab(int inventoryId, string name, string materialLevelReportingGroup)
        {
            var group = NormalizeGroup(materialLevelReportingGroup);

            return new InventoryModel(null)
            {
                Id = inventoryId,
                Key = CreateKey(inventoryId, group),
                InventoryId = inventoryId,
                MaterialLevelReportingGroup = group,
                Name = name
            };
        }

        private List<IUserHiddenMaterialLevelTab> GetHiddenTabsFromDatabase()
        {
            return m_database.SelectFrom<IUserHiddenMaterialLevelTab>().Join(w => w.Inventory)
                .Where(w => w.UserId == m_session.User.Id)
                .Where(w => w.Inventory.ProjectId == m_session.Project.Id)
                .Execute()
                .ToList();
        }

        private IUserHiddenMaterialLevelTab GetHiddenTab(int inventoryId, string materialLevelReportingGroup)
        {
            var normalizedGroup = NormalizeGroup(materialLevelReportingGroup);

            return GetHiddenTabsFromDatabase()
                .FirstOrDefault(w => w.InventoryId == inventoryId
                                     && string.Equals(NormalizeGroup(w.MaterialLevelReportingGroup), normalizedGroup, StringComparison.InvariantCultureIgnoreCase));
        }

        private bool IsSameTab(IUserHiddenMaterialLevelTab hiddenTab, InventoryModel tab)
        {
            return hiddenTab.InventoryId == tab.InventoryId
                   && string.Equals(NormalizeGroup(hiddenTab.MaterialLevelReportingGroup), NormalizeGroup(tab.MaterialLevelReportingGroup), StringComparison.InvariantCultureIgnoreCase);
        }

        private IEnumerable<string> GetTabCacheKeys()
        {
            yield return GetVisibleTabsCacheKey();
            yield return GetHiddenTabsCacheKey();
            yield return $"matLevelInventories_{m_session.Project.Id}:{m_session.User.Id}";
        }

        private string GetVisibleTabsCacheKey()
        {
            return $"materialLevelVisibleTabs_{m_session.Project.Id}:{m_session.User.Id}";
        }

        private string GetHiddenTabsCacheKey()
        {
            return $"materialLevelHiddenTabs_{m_session.Project.Id}:{m_session.User.Id}";
        }

        private static string CreateKey(int inventoryId, string materialLevelReportingGroup)
        {
            return $"{inventoryId}|{NormalizeGroup(materialLevelReportingGroup) ?? string.Empty}";
        }

        private static string NormalizeGroup(string materialLevelReportingGroup)
        {
            return string.IsNullOrWhiteSpace(materialLevelReportingGroup) ? null : materialLevelReportingGroup.Trim();
        }
    }
}
