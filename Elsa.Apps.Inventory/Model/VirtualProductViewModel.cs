using System.Linq;

using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Apps.Inventory.Model
{
    public class VirtualProductViewModel
    {
        public VirtualProductViewModel(IVirtualProduct virtualProduct)
        {
            VirtualProductId = virtualProduct.Id;
            Name = $"#{virtualProduct.Name}";
            HasMaterial = virtualProduct.Materials.Any();
        }

        public int VirtualProductId { get; set; }

        public string Name { get; set; }

        public bool HasMaterial { get; set; }

        public string MaterialsText
        {
            get; set;
        }
    }
}
