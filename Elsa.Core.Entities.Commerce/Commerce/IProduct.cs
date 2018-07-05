using Elsa.Core.Entities.Commerce.Common;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Commerce
{
    [Entity]
    public interface IProduct
    {
        int Id { get; }

        int ProjectId { get; set; }

        [NVarchar(255, false)]
        string Name { get; set; }

        IProject Project { get; }
    }
}
