using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Commerce.Core
{
    public interface IOrderPaymentHelper
    {
        PaymentMethodType GetPaymentMethodType(IPurchaseOrder order);
    }

    public enum PaymentMethodType
    {
        BankTransfer,
        PayOnDelivery,
        Invoice,
        Card,
        PayPal
    }
}
