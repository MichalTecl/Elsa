using Elsa.Core.Entities.Commerce.Core;
using Elsa.Core.Entities.Commerce.Integration;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Inventory
{
    [Entity]
    public interface IVirtualProductOrderItemMapping : IProjectRelatedEntity, IMappedToOrderItem
    {
        int Id { get; }

        int VirtualProductId { get; set; }

        IVirtualProduct VirtualProduct { get; }
    }
}
