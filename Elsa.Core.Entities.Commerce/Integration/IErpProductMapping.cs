using Elsa.Core.Entities.Commerce.Commerce;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Integration
{
    [Entity]
    public interface IErpProductMapping
    {
        int Id { get; }
  
        int ErpId { get; set; }

        IErp Erp { get; }

        int ProductId { get; set; }

        IProduct Product { get; }

        [NVarchar(255, true)]
        string ErpProductId { get; set; }

        [NVarchar(255, true)]
        string ErpProductName { get; set; }

        [NVarchar(255, true)]
        string ErpProductLink { get; set; }
    }
}
