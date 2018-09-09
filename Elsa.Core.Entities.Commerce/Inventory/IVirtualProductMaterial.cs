using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Inventory
{
    [Entity]
    public interface IVirtualProductMaterial
    {
        int Id { get; }

        int VirtualProductId { get; set; }
        IVirtualProduct VirtualProduct { get; }

        int MaterialId { get; set; }
        IMaterial Material { get; }
    }
}
