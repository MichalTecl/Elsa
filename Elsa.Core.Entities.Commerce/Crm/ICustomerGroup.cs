using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Crm
{
    [Entity]
    public interface ICustomerGroup : IIntIdEntity
    {
        int CustomerId { get; set; }
        ICustomer Customer { get; }

        [NVarchar(300, false)]
        string ErpGroupName { get; set; }
    }
}
