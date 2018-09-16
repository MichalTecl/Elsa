using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Inventory
{
    [Entity]
    public interface IVirtualProductMaterial : IMaterialCompositionBase
    {
        int Id { get; }

        int VirtualProductId { get; set; }
        IVirtualProduct VirtualProduct { get; }
    }
}
