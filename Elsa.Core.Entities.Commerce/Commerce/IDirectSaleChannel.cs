using Elsa.Core.Entities.Commerce.Common;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Commerce
{
    [Entity]
    public interface IDirectSaleChannel
    {
        int Id { get; }
        int ProjectId { get; set; }

        IProject Project { get; }

        [NVarchar(255, false)]
        string Name { get; set; }
    }
}
