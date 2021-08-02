using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Commerce.Core
{
    public interface IOrderWeightCalculator
    {
        decimal? GetWeight(IPurchaseOrder po);
    }
}
