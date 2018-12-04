using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Inventory.ProductionSteps;

namespace Elsa.Commerce.Core.VirtualProducts.Model
{
    public class MaterialProductionStepMaterialModel
    {
        public MaterialProductionStepMaterialModel(IMaterialProductionStepMaterial entity)
        {
            Id = entity.Id;
            MaterialId = entity.MaterialId;
            MaterialName = entity.Material.Name;
            Amount = entity.Amount;
            Unit = entity.Unit.Symbol;
        }

        public int Id { get; }

        public int MaterialId { get; }

        public string MaterialName { get; }

        public decimal Amount { get; }

        public string Unit { get; }
    }
}
