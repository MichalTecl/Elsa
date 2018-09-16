using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Inventory
{
    [Entity]
    public interface IVirtualProduct : IProjectRelatedEntity
    {
        int Id { get; }

        [NVarchar(255, false)]
        string Name { get; set; }

        IEnumerable<IVirtualProductMaterial> Materials { get; }
    }
}
