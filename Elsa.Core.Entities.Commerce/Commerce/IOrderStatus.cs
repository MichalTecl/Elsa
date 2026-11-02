using RobOrm.Core;
using RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Commerce
{
    [Entity]
    public interface IOrderStatus
    {
        int Id { get; set; }

        [NVarchar(64, false)]
        string Name { get; set; }
    }
}
