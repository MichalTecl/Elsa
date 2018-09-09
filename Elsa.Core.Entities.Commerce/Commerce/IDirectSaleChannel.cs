using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Commerce
{
    [Entity]
    public interface IDirectSaleChannel : IProjectRelatedEntity
    {
        int Id { get; }

        [NVarchar(255, false)]
        string Name { get; set; }
    }
}
