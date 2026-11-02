using Elsa.Core.Entities.Commerce.Commerce;

using RobOrm.Core;
using RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Integration
{
    [Entity]
    public interface IErpOrderStatusMapping
    {
        int Id { get; }

        int ErpId { get; set; }

        IErp Erp { get; }
        
        [NotFk]
        [NVarchar(64, false)]
        string ErpStatusId { get; set; }

        int OrderStatusId { get; set; }

        IOrderStatus OrderStatus { get; }

        bool? SetPaidInErp { get; set; }
    }
}
