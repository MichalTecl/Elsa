using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Accounting
{
    [Entity]
    public interface IFixedCostType : IProjectRelatedEntity, IIntIdEntity
    {
        [NVarchar(250, false)]
        string Name { get; set; }

        int PercentToDistributeAmongProducts { get; set; }
    }
}
