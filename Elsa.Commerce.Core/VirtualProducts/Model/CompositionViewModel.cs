using System.Text;

using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Commerce.Core.VirtualProducts.Model
{
    public class CompositionViewModel
    {
        public CompositionViewModel(IExtendedMaterialModel material, decimal amount, IMaterialUnit unit, int depth)
        {
            Material = material;
            Amount = amount;
            Unit = unit;
            Depth = depth;
        }

        public IExtendedMaterialModel Material { get; }

        public IMaterialUnit Unit { get; }

        public decimal Amount { get; }

        public int Depth { get; }

        public void Print(StringBuilder target, string depthLevelTrim)
        {
            if (target.Length > 0)
            {
                target.AppendLine();
            }

            for (var i = 0; i < Depth; i++)
            {
                target.Append(depthLevelTrim);
            }

            target.Append(Amount);
            target.Append(Unit.Symbol);
            target.Append(" ");
            target.Append(Material.Name);

        }
    }
}
