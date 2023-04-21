using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Integration
{
    [Entity]
    public interface IShipmentProviderLookup : IIntIdEntity, IProjectRelatedEntity
    {
        [NVarchar(1000, false)]
        string ShipMethodWildcardPattern { get; set; }

        [NVarchar(100, false)]
        string ProviderName { get; set; }
    }
}
