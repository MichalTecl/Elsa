using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Common.Security
{
    [Entity]
    public interface IUserRight
    {
        int Id { get; }

        [NVarchar(255, false)]
        string Symbol { get; set; }

        [NVarchar(-1, false)]
        string Description { get; set; }

        [NVarchar(1024, false)]
        string FullPath { get; set; }
    }
}
