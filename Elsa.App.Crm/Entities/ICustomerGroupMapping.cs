using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.App.Crm.Entities
{
    [Entity]
    public interface ICustomerGroupMapping : IIntIdEntity, IProjectRelatedEntity
    {
        [NVarchar(500, false)]
        string GroupErpName { get; set; }

        [NVarchar(500, false)]
        string ReportingName { get; set; }

        [NVarchar(500, false)]
        string MarginText { get; set; }
    }
}
