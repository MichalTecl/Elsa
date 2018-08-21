
namespace Elsa.App.Commerce.Payments.Models
{
    public class SuggestedPairModel
    {
        public SuggestedPairModel(OrderViewModel order, PaymentViewModel payment)
        {
            Order = order;
            Payment = payment;
        }

        public OrderViewModel Order { get; }
        public PaymentViewModel Payment { get; }
    }
}
