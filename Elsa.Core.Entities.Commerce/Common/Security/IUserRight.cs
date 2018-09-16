using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Common.Security
{
    [Entity]
    public interface IUserRight
    {
        int Id { get; }

        int? ProjectId { get; set; }

        IProject Project { get; }

        [NVarchar(255, false)]
        string Symbol { get; set; }

        [NVarchar(-1, false)]
        string Description { get; set; }
    }
}
