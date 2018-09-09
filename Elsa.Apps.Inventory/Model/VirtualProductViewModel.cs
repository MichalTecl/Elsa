using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Apps.Inventory.Model
{
    public class VirtualProductViewModel
    {
        public VirtualProductViewModel(IVirtualProduct virtualProduct)
        {
            VirtualProductId = virtualProduct.Id;
            Name = virtualProduct.Name;
        }

        public int VirtualProductId { get; set; }

        public string Name { get; set; }
    }
}
