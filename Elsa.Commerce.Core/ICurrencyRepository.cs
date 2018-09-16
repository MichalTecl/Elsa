using Elsa.Core.Entities.Commerce.Common;

namespace Elsa.Commerce.Core
{
    public interface ICurrencyRepository
    {
        ICurrency GetCurrency(string symbol);

        void SaveCurrency(ICurrency currency);
    }
}
