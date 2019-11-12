using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Commerce.Core.Warehouse.Thresholds;
using Elsa.Common;
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

        public IExtendedMaterialModel ProcessMaterialEditRequest(
            int? materialId,
            string name,
            string nominalAmountText,
            int materialInventoryId,
            bool automaticBatches,
            bool requiresPrice,
            bool requiresIncvoice, 
            bool requiresSupplierReference,
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
            var componentEntries = components.Select(i => MaterialEntry.Parse(i)).ToList();

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
                    requiresIncvoice,
                    requiresSupplierReference);

                var toDelete =
                    material.Components.Where(
                        existing =>
                            !componentEntries.Any(
                                e =>
                                    e.MaterialName.Equals(
                                        existing.Material.Name,
                                        StringComparison.InvariantCultureIgnoreCase))).ToList();
                
                using (var materialRepository = m_materialRepository.GetWithPostponedCache())
                {
                    foreach (var del in toDelete)
                    {
                        m_materialRepository.DetachMaterialComponent(material.Id, del.Material.Id);
                    }

                    var matNames = new HashSet<string>();

                    foreach (var componentEntry in componentEntries)
                    {
                        var componentMaterial = materialRepository.GetMaterialByName(componentEntry.MaterialName);
                        if (componentMaterial == null)
                        {
                            throw new ArgumentException($"Neznámý materiál \"{componentEntry.MaterialName}\"");
                        }

                        var componentUnit = m_unitRepository.GetUnitBySymbol(componentEntry.UnitName);
                        if (componentUnit == null)
                        {
                            throw new ArgumentException($"Neznámá měrná jednotka \"{componentEntry.UnitName}\"");
                        }

                        if (!m_conversionHelper.AreCompatible(componentUnit.Id, componentMaterial.NominalUnit.Id))
                        {
                            throw new ArgumentException($"Nelze použít jednotku \"{componentUnit.Symbol}\" pro materiál \"{componentMaterial.Name}\", protože \"{componentUnit.Symbol}\" není plně kompatibilní s \"{componentMaterial.NominalUnit.Symbol}\".");
                        }

                        if (componentMaterial.Id == material.Id)
                        {
                            throw new ArgumentException("Materiál nesmí mít ve složení sám sebe");
                        }

                        if (!matNames.Add(componentEntry.MaterialName))
                        {
                            throw new ArgumentException($"Ve složení materiálu musí být každý materiál jedinečný - duplicita \"{componentEntry.MaterialName}\"");
                        }

                        var flat = componentMaterial.Flatten();
                        if (flat.Any(f => f.Material.Id == material.Id))
                        {
                            throw new ArgumentException($"Není možné použít ve složení materiál {componentMaterial.Name} , protože obsahuje ve struktuře vlastního složení {material.Name}");
                        }

                        materialRepository.SetMaterialComponent(material.Id, componentMaterial.Id, componentEntry.Amount, componentUnit.Id);
                    }
                }

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

        private MaterialSetupInfo MapMaterialInfo(IExtendedMaterialModel material)
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
                            RequiresSupplierReference = material.RequiresSupplierReference
                        };

            if (material.AutomaticBatches)
            {
                var baseName = $"{StringUtil.ConvertToBaseText(material.Name, '_', '_', 3)}_{DateTime.Now:yyyyMMdd}";
                var versionedName = baseName;

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

            return model;
        }

        public IEnumerable<MaterialSetupInfo> GetAllMaterialInfo()
        {
            foreach (var material in m_materialRepository.GetAllMaterials(null))
            {
                yield return MapMaterialInfo(material);
            }
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
