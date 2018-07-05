using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Common
{
    [Entity]
    public interface ICurrency
    {
        int Id { get; }
        int ProjectId { get; set; }
        [NVarchar(8, false)]
        string Symbol { get; set; }
        bool IsProjectMainCurrency { get; set; }
    }
}
