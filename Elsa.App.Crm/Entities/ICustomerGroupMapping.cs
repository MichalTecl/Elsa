using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;
using RobOrm.Core;
using RobOrm.SqlServer.Attributes;

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

        int MarginPriority { get; set; }
    }
}
