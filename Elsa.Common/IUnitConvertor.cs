using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Common
{
    public interface IUnitConvertor
    {
        decimal ConvertAmount(int sourceUnitId, int targetUnitId, decimal sourceAmount);
    }
}
