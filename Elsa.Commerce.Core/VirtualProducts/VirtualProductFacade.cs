using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts.Model;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.VirtualProducts
{
    public class VirtualProductFacade : IVirtualProductFacade
    {
        private readonly IVirtualProductRepository m_virtualProductRepository;
        private readonly IMaterialRepository m_materialRepository;
        private readonly IUnitRepository m_unitRepository;
        private readonly IUnitConversionHelper m_unitConversionHelper;
        private readonly IDatabase m_database;

        public VirtualProductFacade(IVirtualProductRepository virtualProductRepository, IMaterialRepository materialRepository, IUnitRepository unitRepository, IUnitConversionHelper unitConversionHelper, IDatabase database)
        {
            m_virtualProductRepository = virtualProductRepository;
            m_materialRepository = materialRepository;
            m_unitRepository = unitRepository;
            m_unitConversionHelper = unitConversionHelper;
            m_database = database;
        }

        public void ProcessVirtualProductEditRequest(int? virtualProductId, string name, string[] materialEntries)
        {
            using (var tx = m_database.OpenTransaction())
            {
                var requestMaterials = ProcessMaterialEntries(materialEntries).ToList();

                var cnt =
                    requestMaterials
                        .GroupBy(m => m.Material.Name)
                        .Select(g => new { Material = g.Key, Count = g.Count() })
                        .FirstOrDefault(g => g.Count > 1);

                if (cnt != null)
                {
                    throw new ArgumentException($"Materiál \"{cnt.Material}\" nesmí být k tagu připojen více než jednou");
                }
                
                var vp = m_virtualProductRepository.CreateOrUpdateVirtualProduct(virtualProductId, name);

                using (var materialRepository = m_materialRepository.GetWithPostponedCache())
                {

                    var existingMaterials = m_materialRepository.GetMaterialsByVirtualProductId(vp.Id).ToList();

                    //disconnections:
                    var toDetach = existingMaterials.Where(m => requestMaterials.All(rqm => rqm.Material.Id != m.Material.Id))
                            .ToList();

                    foreach (var dtch in toDetach)
                    {
                        materialRepository.DetachMaterial(vp.Id, dtch.Material.Id);
                        existingMaterials.Remove(dtch);
                    }

                    //updates:
                    foreach (var requestComponent in requestMaterials)
                    {
                        materialRepository.AddOrUpdateComponent(
                            vp.Id,
                            requestComponent.Material.Id,
                            requestComponent.Amount,
                            requestComponent.Unit.Id);
                    }
                }
                
                tx.Commit();

                m_virtualProductRepository.CleanCache();
            }
        }

        private IEnumerable<MaterialComponent> ProcessMaterialEntries(string[] materialEntries)
        {
            foreach (var textEntry in materialEntries.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                var entry = MaterialEntry.Parse(textEntry);

                if (!(entry.Amount > 0m))
                {
                    throw new ArgumentException($"Chybné množství {entry.Amount} - musí být větší než 0");
                }

                var material = m_materialRepository.GetMaterialByName(entry.MaterialName);
                if (material == null)
                {
                    throw new ArgumentException($"Materiál \"{entry.MaterialName}\" neexistuje");
                }

                var entryUnit = m_unitRepository.GetUnitBySymbol(entry.UnitName);
                if (entryUnit == null)
                {
                    throw new ArgumentException($"Neznámá jednotka \"{entry.UnitName}\"");
                }

                if (!m_unitConversionHelper.AreCompatible(material.NominalUnit.Id, entryUnit.Id))
                {
                    throw new ArgumentException($"Pro materiál \"{entry.MaterialName}\" nelze použít jednotku \"{entry.UnitName}\", protože není převoditelná na nominální jednotku materiálu \"{material.NominalUnit.Symbol}\"");
                }

                yield return new MaterialComponent(entryUnit, material, entry.Amount, null);
            }
        }
    }
}
