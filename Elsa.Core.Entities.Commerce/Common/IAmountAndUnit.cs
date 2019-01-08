using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Core.Entities.Commerce.Common
{
    public interface IAmountAndUnit
    {
        int UnitId { get; set; }
        IMaterialUnit Unit { get; }
        decimal Amount { get; set; }
    }
}
