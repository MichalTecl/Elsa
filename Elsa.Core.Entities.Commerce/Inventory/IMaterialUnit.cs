using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Inventory
{
    [Entity]
    public interface IMaterialUnit : IProjectRelatedEntity
    {
        int Id { get; }

        [NVarchar(10, false)]
        string Symbol { get; set; }

        bool IntegerOnly { get; set; }
    }
}
