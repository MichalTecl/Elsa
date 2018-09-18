using Elsa.Commerce.Core.VirtualProducts.Model;

namespace Elsa.Apps.Inventory.Model
{
    public class MaterialCompositionInfo : MaterialInfo
    {
        public MaterialCompositionInfo(IExtendedMaterialModel m)
            : base(m)
        {
        }

        public int CompositionId { get; set; }

        public decimal Amount { get; set; }

        public int UnitId { get; set; }

        public string UnitSymbol { get; set; }
    }
}
