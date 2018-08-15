using Elsa.Core.Entities.Commerce.Commerce;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Integration
{
    [Entity]
    public interface IErpProductMapping
    {
        int Id { get; }

        [NotFk]
        [NVarchar(255, false)]
        string ErpProductId { get; set; }

        int ProductId { get; set; }

        IProduct Product { get; }

        int ErpId { get; set; }

        IErp Erp { get; }
    }
}
