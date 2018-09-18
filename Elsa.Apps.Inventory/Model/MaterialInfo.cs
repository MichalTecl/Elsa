using Elsa.Commerce.Core.VirtualProducts.Model;

namespace Elsa.Apps.Inventory.Model
{
    public class MaterialInfo
    {
        public MaterialInfo(IExtendedMaterialModel m)
        {
            Id = m.Id;
            Name = m.Name;
        }

        public int Id { get; set; }

        public string Name { get; set; }
    }
}
