using Elsa.Core.Entities.Commerce.Core;
using Elsa.Core.Entities.Commerce.Integration;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Inventory
{
    [Entity]
    public interface IVirtualProductOrderItemMapping : IProjectRelatedEntity
    {
        int Id { get; }

        int? ErpId { get; set; }
        IErp Erp { get; }

        [NotFk]
        [NVarchar(255, true)]
        string ErpProductId { get; set; }

        [NVarchar(512, true)]
        string ItemName { get; set; }

        int VirtualProductId { get; set; }

        IVirtualProduct VirtualProduct { get; }
    }
}
