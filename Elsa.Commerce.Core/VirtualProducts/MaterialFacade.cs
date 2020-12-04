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
        private readonly IDatabase m_database;
        private readonly IMaterialRepository m_materialRepository;
        private readonly IUnitRepository m_unitRepository;
        private readonly IUnitConversionHelper m_conversionHelper;
        private readonly IVirtualProductRepository m_virtualProductRepository;
        private readonly ISession m_session;
        private readonly IMaterialThresholdRepository m_materialThresholdRepository;

        public MaterialFacade(IDatabase database,
            IMaterialRepository materialRepository,
            IUnitRepository unitRepository,
            IUnitConversionHelper conversionHelper,
            IVirtualProductRepository virtualProductRepository,
            ISession session,
            IMaterialThresholdRepository materialThresholdRepository)
        {
            m_database = database;
            m_materialRepository = materialRepository;
            m_unitRepository = unitRepository;
            m_conversionHelper = conversionHelper;
            m_virtualProductRepository = virtualProductRepository;
            m_session = session;
            m_materialThresholdRepository = materialThresholdRepository;
        }

        public IExtendedMaterialModel ProcessMaterialEditRequest(int? materialId,
            string name,
            string nominalAmountText,
            int materialInventoryId,
            bool automaticBatches,
            bool requiresPrice,
            bool requiresProductionPrice,
            bool requiresIncvoice,
            bool requiresSupplierReference,
            bool autofinalize,
            IEnumerable<string> components,
            string thresholdText)
        {
            name = name?.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Materiál musí mít název");
            }
            
            if (string.IsNullOrWhiteSpace(nominalAmountText))
            {
                throw new ArgumentException("Materiál musí mít vlastní množství a jednotku");
            }
            
            var nominalAmountEntry = MaterialEntry.Parse(nominalAmountText, true);

            var nominalUnit = ValidateAmountUnit(nominalAmountEntry);

            using (var tx = m_database.OpenTransaction())
            {
                var material = m_materialRepository.UpsertMaterial(
                    materialId,
                    name,
                    nominalAmountEntry.Amount,
                    nominalUnit.Id,
                    materialInventoryId,
                    automaticBatches,
                    requiresPrice, 
                    requiresProductionPrice,
                    requiresIncvoice,
                    requiresSupplierReference, autofinalize);
                
                if (thresholdText == null)
                {
                    m_materialThresholdRepository.DeleteThreshold(material.Id);
                }
                else
                {
                    try
                    {
                        var thresholdEntry = MaterialEntry.Parse(thresholdText, true);

                        var thresholdUnit = m_unitRepository.GetUnitBySymbol(thresholdEntry.UnitName);
                        if (thresholdUnit == null)
                        {
                            throw new InvalidOperationException($"Neznámý symbol jednotky \"{thresholdEntry.UnitName}\"");
                        }

                        m_materialThresholdRepository.SaveThreshold(material.Id,
                            thresholdEntry.Amount,
                            thresholdUnit.Id);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Nelze nastavit sledování minimálního množství - {ex.Message}", ex);
                    }
                }

                tx.Commit();
                m_virtualProductRepository.CleanCache();
                m_materialRepository.CleanCache();
                return m_materialRepository.GetMaterialById(material.Id);
            }
        }

        public MaterialSetupInfo GetMaterialInfo(string materialName)
        {
            var material = m_materialRepository.GetMaterialByName(materialName);
            if (material == null)
            {
                return null;
            }

            return MapMaterialInfo(material);
        }

        public MaterialSetupInfo GetMaterialInfo(int materialId)
        {
            var material = m_materialRepository.GetMaterialById(materialId);
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
                            Autofinalization = material.Autofinalization
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
                            m_database.SelectFrom<IMaterialBatch>()
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
            var allMaterials = m_materialRepository.GetAllMaterials(null).ToList();

            var basenames = new HashSet<string>(allMaterials.Where(m => m.AutomaticBatches).Select(m => $"{StringUtil.ConvertToBaseText(m.Name, '_', '_', 3)}_{DateTime.Now:yyyyMMdd}"));

            var mapped = new List<MaterialSetupInfo>(allMaterials.Count);

            using (var tx = m_database.OpenTransaction())
            {
                var takenNames = m_database.SelectFrom<IMaterialBatch>()
                    .Where(mb => mb.ProjectId == m_session.Project.Id).Where(mb => mb.BatchNumber.InCsv(basenames))
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
            var unit = m_unitRepository.GetUnitBySymbol(nominalAmountEntry.UnitName);
            if (unit == null)
            {
                throw new ArgumentException($"Jednotka \"{nominalAmountEntry.UnitName}\" neexistuje");
            }

            return unit;
        }
    }
}
