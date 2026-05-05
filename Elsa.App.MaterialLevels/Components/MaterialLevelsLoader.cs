using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.App.MaterialLevels.Components.Model;
using Elsa.App.MaterialLevels.Entities;
using Elsa.Apps.Inventory;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse.Thresholds;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.EntityComments;
using Elsa.Common.Interfaces;
using Elsa.Common.Utils;
using Robowire.RobOrm.Core;

namespace Elsa.App.MaterialLevels.Components
{
    public class MaterialLevelsLoader : IMaterialLevelsLoader
    {
        private readonly ISession m_session;
        private readonly IDatabase m_database;
        private readonly AmountProcessor m_amountProcessor;
        private readonly IUnitRepository m_unitRepository;
        private readonly IMaterialThresholdRepository m_thresholdRepository;
        private readonly IMaterialRepository m_materialRepository;
        private readonly ICache m_cache;
        private readonly IInventoryWatchRepository m_inventoryWatchRepository;
        private readonly IUserRepository m_userRepository;
        private readonly ISupplierRepository m_supplierRepository;
        private readonly IEntityCommentsRepository m_entityCommentsRepository;
        private readonly IUserNickProvider m_userNickProvider;

        public MaterialLevelsLoader(ISession session, IDatabase database, AmountProcessor amountProcessor,
            IUnitRepository unitRepository, IMaterialThresholdRepository thresholdRepository, ICache cache, IMaterialRepository materialRepository, IInventoryWatchRepository inventoryWatchRepository, IUserRepository userRepository, ISupplierRepository supplierRepository, IEntityCommentsRepository entityCommentsRepository, IUserNickProvider userNickProvider)
        {
            m_session = session;
            m_database = database;
            m_amountProcessor = amountProcessor;
            m_unitRepository = unitRepository;
            m_thresholdRepository = thresholdRepository;
            m_cache = cache;
            m_materialRepository = materialRepository;
            m_inventoryWatchRepository = inventoryWatchRepository;
            m_userRepository = userRepository;
            m_supplierRepository = supplierRepository;
            m_entityCommentsRepository = entityCommentsRepository;
            m_userNickProvider = userNickProvider;
        }

        public IEnumerable<MaterialLevelEntryModel> Load(int inventoryId, string materialLevelReportingGroup)
        {
            var result = new List<MaterialLevelEntryModel>();
            var resultIndex = new Dictionary<int, MaterialLevelEntryModel>();
            var normalizedReportingGroup = NormalizeReportingGroup(materialLevelReportingGroup);

            // user repo cache fill :(
            m_userRepository.GetAllUsers();

            var materialsById = m_materialRepository.GetAllMaterials(inventoryId, true).ToDictionary(m => m.Id);
            var unitsById = m_unitRepository.GetAllUnits().ToDictionary(u => u.Id);
            var thresholdsByMaterialId = m_thresholdRepository.GetAllThresholds().ToDictionary(t => t.MaterialId);
            var commentsByMaterialId = m_session.HasUserRight(InventoryUserRights.MaterialCommentsView)
                ? m_entityCommentsRepository.GetComments("Material", materialsById.Keys)
                : new Dictionary<int, List<EntityComment>>();

            var supplierOrderLimits = m_supplierRepository
                .GetSuppliers()
                .Where(s => (s.OrderFulfillDays ?? 0) > 0)
                .GroupBy(s => s.Name)
                .ToDictionary(s => s.Key, s => new { Lim = s.Min(t => t.OrderFulfillDays ?? 9999), Name = s.Min(x => x.Name) });

            m_database.Sql().Call("GetMaterialLevelsReport").WithParam("@inventoryId", inventoryId).WithParam("@projectId", m_session.Project.Id).WithParam("@materialLevelReportingGroup", normalizedReportingGroup)
                .ReadRows<int, string, string, int, decimal, string, string, string, DateTime?, int?, DateTime?>((materialId, materialName, batchNumber, unitId, available, supName, supMail, supPhone, orderDt, orderUserId, deliveryDeadline)=>
                {
                    if (!resultIndex.TryGetValue(materialId, out var entry))
                    {
                        entry = new MaterialLevelEntryModel();
                        result.Add(entry);
                        resultIndex[materialId] = entry;

                        entry.MaterialId = materialId;
                        entry.MaterialName = materialName;

                        if (!m_session.HasUserRight(InventoryUserRights.ViewSuppliers)) 
                        {
                            supName = supMail = supPhone = string.Empty;
                        }

                        entry.SupplierName = supName;
                        entry.SupplierEmail = supMail;
                        entry.SupplierPhone = supPhone;
                        entry.OrderDt = orderDt?.ToString(MaterialLevelEntryModel.OrderDtViewFormat);
                        entry.RawOrderDt = orderDt;
                        entry.OrderUser = orderUserId == null ? null : m_userRepository.GetUserNick(orderUserId.Value);
                        entry.DeliveryDeadline = deliveryDeadline;
                    }

                    if (available == 0)
                    {
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(batchNumber))
                    {
                        var batchModel = entry.Batches.FirstOrDefault(b =>
                            b.BatchNumber.Equals(batchNumber, StringComparison.InvariantCultureIgnoreCase) && b.UnitId == unitId);

                        if (batchModel == null)
                        {
                            batchModel = new BatchAmountModel
                            {
                                BatchNumber = batchNumber,
                                UnitId = unitId
                            };
                            entry.Batches.Add(batchModel);
                        }

                        batchModel.Value += available;
                    }
                });

            foreach (var r in result)
            {
                var material = materialsById[r.MaterialId];
                ApplyComment(r, commentsByMaterialId);

                if (thresholdsByMaterialId.TryGetValue(r.MaterialId, out var threshold))
                {
                    r.Threshold = new Amount(threshold.ThresholdQuantity, unitsById[threshold.UnitId]);
                }

                foreach (var batch in r.Batches)
                {
                    batch.Amount = new Amount(batch.Value, unitsById[batch.UnitId]);
                }

                r.Total = m_amountProcessor.Sum(r.Batches.Select(b => b.Amount));

                if (string.IsNullOrWhiteSpace(r.UnitSymbol))
                {                    
                    r.DefaultUnitSymbol = material.NominalUnit.Symbol;
                }

                var isLow = r.Threshold != null && m_amountProcessor.GreaterThan(r.Threshold, r.Total ?? r.Threshold.ToZeroAmount());

                if (!isLow)
                {
                    // there is enough
                    r.WarningLevel = WarningLevel.None;
                }
                else if(!string.IsNullOrEmpty(r.OrderDt))
                {
                    // we are under threshold, but another batch is ordered
                    r.WarningLevel = WarningLevel.Low;

                    string limitInfo = null;
                    int ffLimitDays = 99999;
                    if ((material.OrderFulfillDays ?? 0) > 0)
                    {
                        limitInfo = $"Limit je {material.OrderFulfillDays} dnů pro materiál {material.Name}";
                        ffLimitDays = material.OrderFulfillDays.Value;
                    }
                    else if (supplierOrderLimits.TryGetValue(r.SupplierName, out var supplierInfo))
                    {
                        limitInfo = $"Limit je {supplierInfo.Lim} dnů pro dodavatele {supplierInfo.Name}";
                        ffLimitDays = supplierInfo.Lim;
                    }
                    else
                    {
                        limitInfo = "Dodavatel ani materiál nemají nastavený limit pro naskladnění - za limit se považuje 30 dnů";
                        ffLimitDays = 30;
                    }

                    var orderDelayLimit = r.RawOrderDt.Value.AddDays(ffLimitDays);

                    if (r.DeliveryDeadline != null)
                    {
                        limitInfo = $"Limit naskladnění byl ručně nastaven na {StringUtil.FormatDate(r.DeliveryDeadline)} pro objednávku ze dne {r.OrderDt}";
                        orderDelayLimit = r.DeliveryDeadline.Value;
                    }

                    if (orderDelayLimit < DateTime.Now)
                    {
                        r.DelayedOrderMessage = $"Naskladnění bylo očekáváno do {StringUtil.FormatDate(orderDelayLimit)}. ({limitInfo})";
                        r.WarningLevel = WarningLevel.High;
                        r.DelayedOrder = true;
                    }
                    else
                    {
                        r.DelayedOrderMessage = limitInfo;
                    }
                }
                else
                {
                    // we are under threshold
                    r.WarningLevel = WarningLevel.High;
                }
            }

            return result.OrderByDescending(r => (int)r.WarningLevel).ThenBy(r => r.MaterialName);
        }

        public IEnumerable<InventoryModel> GetInventories()
        {
            var cacheKey = $"matLevelInventories_{m_session.Project.Id}:{m_session.User.Id}";
            
            return m_cache.ReadThrough(cacheKey, TimeSpan.FromMinutes(5), () =>
            {
                var res = GetInventory();

                LoadWarnings(res);

                return res;
            });
        }

        private List<InventoryModel> GetInventory()
        {
            var result = new List<InventoryModel>();

            var visibleTabs = m_inventoryWatchRepository.GetVisibleTabs();
            if (visibleTabs.Count == 0)
            {
                return result;
            }

            var aggreagate = new InventoryModel(null)
            {
                Id = -1,
                Key = "-1|",
                InventoryId = -1,
                Name = "Varování"
            };

            result.Add(aggreagate);

            result.AddRange(visibleTabs.Select(i => new InventoryModel(aggreagate)
            {
                Id = i.Id,
                Key = i.Key,
                InventoryId = i.InventoryId,
                MaterialLevelReportingGroup = i.MaterialLevelReportingGroup,
                Name = i.Name
            }));

            return result;
        }

        private void LoadWarnings(List<InventoryModel> models)
        {
            var allUnits = m_unitRepository.GetAllUnits().ToDictionary(u => u.Id);

            m_database.Sql().Call("GetThresholdsState")
                .WithParam("@projectId", m_session.Project.Id)
                .WithParam("@userId", m_session.User.Id)
                .ReadRows<int, string, int, string, decimal, int, decimal>(
                    (inventoryId, materialLevelReportingGroup, materialId, materialName, threshodlQty, unitId, available) =>
                    {
                        var normalizedGroup = NormalizeReportingGroup(materialLevelReportingGroup);
                        var inv = models.FirstOrDefault(m => m.InventoryId == inventoryId
                                                             && string.Equals(NormalizeReportingGroup(m.MaterialLevelReportingGroup), normalizedGroup, StringComparison.InvariantCultureIgnoreCase));
                        if (inv == null)
                        {
                            return;
                        }

                        var unit = allUnits[unitId];
                        inv.AddWarning(materialName, new Amount(available, unit));
                    });

            foreach (var m in models)
            {
                m.Close();
            }
        }

        private static string NormalizeReportingGroup(string materialLevelReportingGroup)
        {
            return string.IsNullOrWhiteSpace(materialLevelReportingGroup) ? null : materialLevelReportingGroup.Trim();
        }

        private void ApplyComment(MaterialLevelEntryModel entry, Dictionary<int, List<EntityComment>> commentsByMaterialId)
        {
            if (!commentsByMaterialId.TryGetValue(entry.MaterialId, out var comments))
            {
                return;
            }

            var comment = comments.OrderByDescending(c => c.Id).FirstOrDefault();
            if (comment == null)
            {
                return;
            }

            entry.CommentDt = comment.PostDt;
            entry.CommentText = comment.Text;
            entry.CommentAuthorNick = m_userNickProvider.GetUserNick(comment.Author.Id);
        }
    }
}
