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

        public MaterialLevelsLoader(ISession session, IDatabase database, AmountProcessor amountProcessor,
            IUnitRepository unitRepository, IMaterialThresholdRepository thresholdRepository, ICache cache, IMaterialRepository materialRepository, IInventoryWatchRepository inventoryWatchRepository, IUserRepository userRepository, ISupplierRepository supplierRepository)
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
        }

        public IEnumerable<MaterialLevelEntryModel> Load(int inventoryId)
        {
            var result = new List<MaterialLevelEntryModel>();

            // user repo cache fill :(
            m_userRepository.GetAllUsers();

            var supplierOrderLimits = m_supplierRepository.GetSuppliers().GroupBy(s => s.Name).ToDictionary(s => s.Key, s => new { Lim = s.Min(t => t.OrderFulfillDays ?? 9999), Name = s.Min(x => x.Name) });

            m_database.Sql().Call("GetMaterialLevelsReport").WithParam("@inventoryId", inventoryId).WithParam("@projectId", m_session.Project.Id)
                .ReadRows<int,string, string, int, decimal, string, string, string, DateTime?, int?>((materialId, materialName, batchNumber, unitId, available, supName, supMail, supPhone, orderDt, orderUserId)=>
                {
                    var entry = result.FirstOrDefault(r => r.MaterialId == materialId);
                    if (entry == null)
                    {
                        entry = new MaterialLevelEntryModel();
                        result.Add(entry);

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
                    }

                    if (available == 0)
                    {
                        return;
                    }

                    var batchModel = entry.Batches.FirstOrDefault(b =>
                        b.BatchNumber.Equals(batchNumber, StringComparison.InvariantCultureIgnoreCase) && b.UnitId == unitId);

                    if (!string.IsNullOrWhiteSpace(batchNumber))
                    {
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
                var threshold = m_thresholdRepository.GetThreshold(r.MaterialId);
                if (threshold != null)
                {
                    r.Threshold = new Amount(threshold.ThresholdQuantity, m_unitRepository.GetUnit(threshold.UnitId));
                }

                foreach (var batch in r.Batches)
                {
                    batch.Amount = new Amount(batch.Value, m_unitRepository.GetUnit(batch.UnitId));
                }

                r.Total = m_amountProcessor.Sum(r.Batches.Select(b => b.Amount));

                if (string.IsNullOrWhiteSpace(r.UnitSymbol))
                {
                    var material = m_materialRepository.GetMaterialById(r.MaterialId);
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

                    DateTime orderDelaylimit;
                    // but maybe the order is delayed?
                    if (supplierOrderLimits.TryGetValue(r.SupplierName, out var supplierInfo) 
                    && ((orderDelaylimit = r.RawOrderDt.Value.AddDays(supplierInfo.Lim)) < DateTime.Now))
                    {                        
                        r.DelayedOrderMessage = $"Naskladnění bylo očekáváno do {StringUtil.FormatDate(orderDelaylimit)}. (Limit je {supplierInfo.Lim} dnů pro dodavatele {supplierInfo.Name})";
                        r.WarningLevel = WarningLevel.High;
                        r.DelayedOrder = true;
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
            var cacheKey = $"matLevelInventories_{m_session.Project.Id}";
            
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

            var watched = m_inventoryWatchRepository.GetWatchedInventories();
            if (watched.Count == 0)
            {
                return result;
            }

            var aggreagate = new InventoryModel(null)
            {
                Id = -1,
                Name = "Varování"
            };

            result.Add(aggreagate);

            result.AddRange(watched.OrderBy(i => i.CanBeConnectedToTag ? 0 : 1).ThenBy(i => i.Name).Select(i => new InventoryModel(aggreagate)
            {
                Id = i.Id,
                Name = i.Name
            }));

            return result;
        }

        private void LoadWarnings(List<InventoryModel> models)
        {
            var allUnits = m_unitRepository.GetAllUnits();

            m_database.Sql().Call("GetThresholdsState")
                .WithParam("@projectId", m_session.Project.Id)
                .WithParam("@userId", m_session.User.Id)
                .ReadRows<int, int, string, decimal, int, decimal>(
                    (inventoryId, materialId, materialName, threshodlQty, unitId, available) =>
                    {
                        var inv = models.FirstOrDefault(m => m.Id == inventoryId);
                        if (inv == null)
                        {
                            return;
                        }

                        var unit = allUnits.FirstOrDefault(u => u.Id == unitId).Ensure();
                        inv.AddWarning(materialName, new Amount(available, unit));
                    });

            foreach (var m in models)
            {
                m.Close();
            }
        }
    }
}
