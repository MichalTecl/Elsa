using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Common;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Inventory;

using RobOrm.Core;

namespace Elsa.Commerce.Core.VirtualProducts
{
    public class VirtualProductFacade : IVirtualProductFacade
    {
        private readonly IVirtualProductRepository _virtualProductRepository;
        private readonly IMaterialRepository _materialRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IUnitConversionHelper _unitConversionHelper;
        private readonly IDatabase _database;

        public VirtualProductFacade(IVirtualProductRepository virtualProductRepository, IMaterialRepository materialRepository, IUnitRepository unitRepository, IUnitConversionHelper unitConversionHelper, IDatabase database)
        {
            _virtualProductRepository = virtualProductRepository;
            _materialRepository = materialRepository;
            _unitRepository = unitRepository;
            _unitConversionHelper = unitConversionHelper;
            _database = database;
        }

        public IVirtualProduct ProcessVirtualProductEditRequest(int? virtualProductId, string name, string[] materialEntries)
        {
            using (var tx = _database.OpenTransaction())
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
                
                var vp = _virtualProductRepository.CreateOrUpdateVirtualProduct(virtualProductId, name);

                using (var materialRepository = _materialRepository.GetWithPostponedCache())
                {

                    var existingMaterials = _materialRepository.GetMaterialsByVirtualProductId(vp.Id).ToList();

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

                _virtualProductRepository.CleanCache();

                return vp;
            }
        }

        public MaterialAmountModel GetOrderItemMaterialForSingleUnit(IPurchaseOrder order, IOrderItem item)
        {
            var tags = _virtualProductRepository.GetVirtualProductsByOrderItem(order, item);

            var materialTags = tags.Where(t => t.Materials.Any()).ToList();
            if (materialTags.Count != 1)
            {
                throw new InvalidOperationException($"Prodejní položka '{item.PlacedName}' je spojena s {materialTags.Count} materiálů, požadovaný počet je 1");
            }

            var material = materialTags.Single().Materials.ToList();
            if (material.Count != 1)
            {
                throw new InvalidOperationException($"Prodejní položka '{item.PlacedName}' je tagem '{materialTags[0].Name}' spojena s {material.Count} materiálů, požadovaný počet je 1");
            }

            return new MaterialAmountModel(material[0].ComponentId, new Amount(material[0].Amount, material[0].Unit));
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

                var material = _materialRepository.GetMaterialByName(entry.MaterialName);
                if (material == null)
                {
                    throw new ArgumentException($"Materiál \"{entry.MaterialName}\" neexistuje");
                }

                var entryUnit = _unitRepository.GetUnitBySymbol(entry.UnitName);
                if (entryUnit == null)
                {
                    throw new ArgumentException($"Neznámá jednotka \"{entry.UnitName}\"");
                }

                if (!_unitConversionHelper.AreCompatible(material.NominalUnit.Id, entryUnit.Id))
                {
                    throw new ArgumentException($"Pro materiál \"{entry.MaterialName}\" nelze použít jednotku \"{entry.UnitName}\", protože není převoditelná na nominální jednotku materiálu \"{material.NominalUnit.Symbol}\"");
                }

                yield return new MaterialComponent(entryUnit, material, entry.Amount, null);
            }
        }
    }
}
