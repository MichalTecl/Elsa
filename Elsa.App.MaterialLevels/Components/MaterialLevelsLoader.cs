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
using RobOrm.Core;

namespace Elsa.App.MaterialLevels.Components
{
    public class MaterialLevelsLoader : IMaterialLevelsLoader
    {
        private readonly ISession _session;
        private readonly IDatabase _database;
        private readonly AmountProcessor _amountProcessor;
        private readonly IUnitRepository _unitRepository;
        private readonly IMaterialThresholdRepository _thresholdRepository;
        private readonly IMaterialRepository _materialRepository;
        private readonly ICache _cache;
        private readonly IInventoryWatchRepository _inventoryWatchRepository;
        private readonly IUserRepository _userRepository;
        private readonly ISupplierRepository _supplierRepository;
        private readonly IEntityCommentsRepository _entityCommentsRepository;
        private readonly IUserNickProvider _userNickProvider;

        public MaterialLevelsLoader(ISession session, IDatabase database, AmountProcessor amountProcessor,
            IUnitRepository unitRepository, IMaterialThresholdRepository thresholdRepository, ICache cache, IMaterialRepository materialRepository, IInventoryWatchRepository inventoryWatchRepository, IUserRepository userRepository, ISupplierRepository supplierRepository, IEntityCommentsRepository entityCommentsRepository, IUserNickProvider userNickProvider)
        {
            _session = session;
            _database = database;
            _amountProcessor = amountProcessor;
            _unitRepository = unitRepository;
            _thresholdRepository = thresholdRepository;
            _cache = cache;
            _materialRepository = materialRepository;
            _inventoryWatchRepository = inventoryWatchRepository;
            _userRepository = userRepository;
            _supplierRepository = supplierRepository;
            _entityCommentsRepository = entityCommentsRepository;
            _userNickProvider = userNickProvider;
        }

        public IEnumerable<MaterialLevelEntryModel> Load(int inventoryId, string materialLevelReportingGroup)
        {
            var result = new List<MaterialLevelEntryModel>();
            var resultIndex = new Dictionary<int, MaterialLevelEntryModel>();
            var normalizedReportingGroup = NormalizeReportingGroup(materialLevelReportingGroup);

            // user repo cache fill :(
            _userRepository.GetAllUsers();

            var materialsById = _materialRepository.GetAllMaterials(inventoryId, true).ToDictionary(m => m.Id);
            var unitsById = _unitRepository.GetAllUnits().ToDictionary(u => u.Id);
            var thresholdsByMaterialId = _thresholdRepository.GetAllThresholds().ToDictionary(t => t.MaterialId);
            var commentsByMaterialId = _session.HasUserRight(InventoryUserRights.MaterialCommentsView)
                ? _entityCommentsRepository.GetComments("Material", materialsById.Keys)
                : new Dictionary<int, List<EntityComment>>();

            var supplierOrderLimits = _supplierRepository
                .GetSuppliers()
                .Where(s => (s.OrderFulfillDays ?? 0) > 0)
                .GroupBy(s => s.Name)
                .ToDictionary(s => s.Key, s => new { Lim = s.Min(t => t.OrderFulfillDays ?? 9999), Name = s.Min(x => x.Name) });

            _database.Sql().Call("GetMaterialLevelsReport").WithParam("@inventoryId", inventoryId).WithParam("@projectId", _session.Project.Id).WithParam("@materialLevelReportingGroup", normalizedReportingGroup)
                .ReadRows<int, string, string, int, decimal, string, string, string, DateTime?, int?, DateTime?>((materialId, materialName, batchNumber, unitId, available, supName, supMail, supPhone, orderDt, orderUserId, deliveryDeadline)=>
                {
                    if (!resultIndex.TryGetValue(materialId, out var entry))
                    {
                        entry = new MaterialLevelEntryModel();
                        result.Add(entry);
                        resultIndex[materialId] = entry;

                        entry.MaterialId = materialId;
                        entry.MaterialName = materialName;

                        if (!_session.HasUserRight(InventoryUserRights.ViewSuppliers)) 
                        {
                            supName = supMail = supPhone = string.Empty;
                        }

                        entry.SupplierName = supName;
                        entry.SupplierEmail = supMail;
                        entry.SupplierPhone = supPhone;
                        entry.OrderDt = orderDt?.ToString(MaterialLevelEntryModel.OrderDtViewFormat);
                        entry.RawOrderDt = orderDt;
                        entry.OrderUser = orderUserId == null ? null : _userRepository.GetUserNick(orderUserId.Value);
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

                r.Total = _amountProcessor.Sum(r.Batches.Select(b => b.Amount)) ?? new Amount(0, material.NominalUnit);
                if (r.Threshold != null)
                {
                    r.Total = _amountProcessor.Convert(r.Total, r.Threshold.Unit);
                }

                if (string.IsNullOrWhiteSpace(r.UnitSymbol))
                {                    
                    r.DefaultUnitSymbol = material.NominalUnit.Symbol;
                }

                var isLow = r.Threshold != null && _amountProcessor.GreaterThan(r.Threshold, r.Total ?? r.Threshold.ToZeroAmount());

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
            var cacheKey = $"matLevelInventories_{_session.Project.Id}:{_session.User.Id}";
            
            return _cache.ReadThrough(cacheKey, TimeSpan.FromMinutes(5), () =>
            {
                var res = GetInventory();

                LoadWarnings(res);

                return res;
            });
        }

        private List<InventoryModel> GetInventory()
        {
            var result = new List<InventoryModel>();

            var visibleTabs = _inventoryWatchRepository.GetVisibleTabs();
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
            var allUnits = _unitRepository.GetAllUnits().ToDictionary(u => u.Id);

            _database.Sql().Call("GetThresholdsState")
                .WithParam("@projectId", _session.Project.Id)
                .WithParam("@userId", _session.User.Id)
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
            entry.CommentAuthorNick = _userNickProvider.GetUserNick(comment.Author.Id);
        }
    }
}
