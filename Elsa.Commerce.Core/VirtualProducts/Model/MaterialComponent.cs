using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Commerce.Core.VirtualProducts.Model
{
    public sealed class MaterialComponent
    {
        public readonly IMaterialUnit Unit;
        public readonly IExtendedMaterialModel Material;
        public decimal Amount;

        public MaterialComponent(IMaterialUnit unit, IExtendedMaterialModel material, decimal amount)
        {
            Unit = unit;
            Material = material;
            Amount = amount;
        }
    }
}
