using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Commerce.Core.Warehouse.Thresholds;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.VirtualProducts
{
    public class MaterialFacade : IMaterialFacade
    {
        private readonly IDatabase _database;
        private readonly IMaterialRepository _materialRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IUnitConversionHelper _conversionHelper;
        private readonly IVirtualProductRepository _virtualProductRepository;
        private readonly ISession _session;
        private readonly IMaterialThresholdRepository _materialThresholdRepository;

        public MaterialFacade(IDatabase database,
            IMaterialRepository materialRepository,
            IUnitRepository unitRepository,
            IUnitConversionHelper conversionHelper,
            IVirtualProductRepository virtualProductRepository,
            ISession session,
            IMaterialThresholdRepository materialThresholdRepository)
        {
            _database = database;
            _materialRepository = materialRepository;
            _unitRepository = unitRepository;
            _conversionHelper = conversionHelper;
            _virtualProductRepository = virtualProductRepository;
            _session = session;
            _materialThresholdRepository = materialThresholdRepository;
        }

        public IExtendedMaterialModel ProcessMaterialEditRequest(MaterialEditRequestModel request)
        {
            var name = request.MaterialName?.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Materiál musí mít název");
            }
            
            if (string.IsNullOrWhiteSpace(request.NominalAmountText))
            {
                throw new ArgumentException("Materiál musí mít vlastní množství a jednotku");
            }
            
            var nominalAmountEntry = MaterialEntry.Parse(request.NominalAmountText, true);

            var nominalUnit = ValidateAmountUnit(nominalAmountEntry);

            using (var tx = _database.OpenTransaction())
            {
                var material = _materialRepository.UpsertMaterial(
                    request.MaterialId,
                    m =>
                    {
                        m.Name = name;
                        m.NominalAmount = nominalAmountEntry.Amount;
                        m.NominalUnitId = nominalUnit.Id;
                        m.InventoryId = request.MaterialInventoryId;
                        m.AutomaticBatches = request.AutomaticBatches;
                        m.RequiresPrice = request.RequiresPrice;
                        m.RequiresProductionPrice = request.RequiresProductionPrice;
                        m.RequiresInvoiceNr = request.RequiresInvoice;
                        m.RequiresSupplierReference = request.RequiresSupplierReference;
                        m.UseAutofinalization = request.Autofinalization;
                        m.CanBeDigitalOnly = request.CanBeDigital;
                        m.DaysBeforeWarnForUnused = request.DaysBeforeWarnForUnused;
                        m.UnusedWarnMaterialType = string.IsNullOrWhiteSpace(request.UnusedWarnMaterialType) ? null : request.UnusedWarnMaterialType.Trim();
                        m.UsageProlongsLifetime = request.UsageProlongsLifetime;
                        m.NotAbandonedUntilNewerBatchUsed = request.NotAbandonedUntilNewerBatchUsed;
                        m.UniqueBatchNumbers = request.UniqueBatchNumbers;
                        m.OrderFulfillDays = request.OrderFulfillDays == 0 ? null : request.OrderFulfillDays;
                        m.ExpirationMonths = request.ExpirationMonths == 0 ? null : request.ExpirationMonths;
                        m.DistributorExpirationLimit = request.DistributorExpirationLimit == 0 ? null : request.DistributorExpirationLimit;
                        m.RetailExpirationLimit = request.RetailExpirationLimit == 0 ? null : request.RetailExpirationLimit;
                    });
                
                if (request.ThresholdText == null)
                {
                    _materialThresholdRepository.DeleteThreshold(material.Id);
                }
                else
                {
                    try
                    {
                        var thresholdEntry = MaterialEntry.Parse(request.ThresholdText, true);

                        var thresholdUnit = _unitRepository.GetUnitBySymbol(thresholdEntry.UnitName);
                        if (thresholdUnit == null)
                        {
                            throw new InvalidOperationException($"Neznámý symbol jednotky \"{thresholdEntry.UnitName}\"");
                        }

                        _materialThresholdRepository.SaveThreshold(material.Id,
                            thresholdEntry.Amount,
                            thresholdUnit.Id);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Nelze nastavit sledování minimálního množství - {ex.Message}", ex);
                    }
                }

                tx.Commit();
                _virtualProductRepository.CleanCache();
                _materialRepository.CleanCache();
                return _materialRepository.GetMaterialById(material.Id);
            }
        }

        public MaterialSetupInfo GetMaterialInfo(string materialName)
        {
            var material = _materialRepository.GetMaterialByName(materialName);
            if (material == null)
            {
                return null;
            }

            return MapMaterialInfo(material);
        }

        public MaterialSetupInfo GetMaterialInfo(int materialId)
        {
            var material = _materialRepository.GetMaterialById(materialId);
            if (material == null)
            {
                return null;
            }

            return MapMaterialInfo(material);
        }

        private MaterialSetupInfo MapMaterialInfo(IExtendedMaterialModel material, List<string> takenNames = null)
        {
            var model = new MaterialSetupInfo
                        {
                            MaterialId = material.Id,
                            PreferredUnitSymbol = material.Adaptee.NominalUnit.Symbol,
                            IsManufactured = material.IsManufactured,
                            MaterialName = material.Name,
                            RequiresInvoice = material.RequiresInvoice,
                            RequiresPrice = material.RequiresInvoice,
                            AutomaticBatches = material.AutomaticBatches,
                            RequiresSupplierReference = material.RequiresSupplierReference,
                            Autofinalization = material.Autofinalization,
                            CanBeDigital = material.CanBeDigital
                        };

            if (material.AutomaticBatches)
            {
                var baseName = $"{StringUtil.ConvertToBaseText(material.Name, '_', '_', 3)}_{DateTime.Now:yyyyMMdd}";
                var versionedName = baseName;

                if (takenNames != null && !takenNames.Contains(versionedName))
                {
                    model.AutoBatchNr = versionedName;
                }
                else
                {
                    for (var i = 1;; i++)
                    {
                        var e =
                            _database.SelectFrom<IMaterialBatch>()
                                .Where(b => b.BatchNumber == versionedName)
                                .Take(1)
                                .Execute()
                                .FirstOrDefault();
                        if (e == null)
                        {
                            break;
                        }

                        versionedName = $"{baseName}.{i}";
                    }

                    model.AutoBatchNr = versionedName;
                }

                takenNames?.Add(versionedName);
            }

            return model;
        }

        public IEnumerable<MaterialSetupInfo> GetAllMaterialInfo()
        {
            var allMaterials = _materialRepository.GetAllMaterials(null, true).ToList();

            var basenames = new HashSet<string>(allMaterials.Where(m => m.AutomaticBatches).Select(m => $"{StringUtil.ConvertToBaseText(m.Name, '_', '_', 3)}_{DateTime.Now:yyyyMMdd}"));

            var mapped = new List<MaterialSetupInfo>(allMaterials.Count);

            using (var tx = _database.OpenTransaction())
            {
                var takenNames = _database.SelectFrom<IMaterialBatch>()
                    .Where(mb => mb.ProjectId == _session.Project.Id).Where(mb => mb.BatchNumber.InCsv(basenames))
                    .Execute()
                    .Select(n => n.BatchNumber)
                    .ToList();

                foreach (var src in allMaterials)
                {
                    mapped.Add(MapMaterialInfo(src, takenNames));
                }

                tx.Commit();
            }

            return mapped;

            //foreach (var material in m_materialRepository.GetAllMaterials(null))
            //{
            //    yield return MapMaterialInfo(material);
            //}
        }

        private IMaterialUnit ValidateAmountUnit(MaterialEntry nominalAmountEntry)
        {
            var unit = _unitRepository.GetUnitBySymbol(nominalAmountEntry.UnitName);
            if (unit == null)
            {
                throw new ArgumentException($"Jednotka \"{nominalAmountEntry.UnitName}\" neexistuje");
            }

            return unit;
        }        
    }
}
