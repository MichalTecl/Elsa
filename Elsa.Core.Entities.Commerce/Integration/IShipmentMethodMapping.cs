using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Integration
{
    [Entity]
    public interface IShipmentMethodMapping : IIntIdEntity, IProjectRelatedEntity
    {
        [NVarchar(500,false)]
        string Source { get; set; }

        [NVarchar(500, false)]
        string Target { get; set; }
    }
}
