using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Commerce.Core
{
    public interface IOrderStatusTranslator
    {
        string Translate(int statusId);

        string Translate(IOrderStatus status);
    }
}
