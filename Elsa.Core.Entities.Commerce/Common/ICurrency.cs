using Elsa.Core.Entities.Commerce.Core;

using RobOrm.Core;
using RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Common
{
    [Entity]
    public interface ICurrency : IProjectRelatedEntity
    {
        int Id { get; }

        [NVarchar(8, false)]
        string Symbol { get; set; }

        bool IsProjectMainCurrency { get; set; }
    }
}
