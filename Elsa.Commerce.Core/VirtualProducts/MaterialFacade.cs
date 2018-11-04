using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Inventory;

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

        public MaterialFacade(IDatabase database, IMaterialRepository materialRepository, IUnitRepository unitRepository, IUnitConversionHelper conversionHelper, IVirtualProductRepository virtualProductRepository)
        {
            m_database = database;
            m_materialRepository = materialRepository;
            m_unitRepository = unitRepository;
            m_conversionHelper = conversionHelper;
            m_virtualProductRepository = virtualProductRepository;
        }

        public IExtendedMaterialModel ProcessMaterialEditRequest(
            int? materialId,
            string name,
            string nominalAmountText,
            int materialInventoryId,
            IEnumerable<string> components)
        {
            name = name.Trim();

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
                    materialInventoryId);

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
                            throw new ArgumentException($"Materiál nesmí mít ve složení sám sebe");
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

                tx.Commit();
                m_virtualProductRepository.CleanCache();
                return m_materialRepository.GetMaterialById(material.Id);
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
