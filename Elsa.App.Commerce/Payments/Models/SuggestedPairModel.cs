
using Elsa.Core.Entities.Commerce.Integration;

namespace Elsa.App.Commerce.Payments.Models
{
    public class SuggestedPairModel
    {
        public SuggestedPairModel(OrderViewModel order, PaymentViewModel payment, IOrderProcessingLog lastPaymentReminder)
        {
            Order = order;
            Payment = payment;
            PaymentReminderInfo = lastPaymentReminder == null
                ? null
                : $"{lastPaymentReminder.Description} {lastPaymentReminder.ProcessDt:dd.MM.yyyy HH:mm}";
        }

        public OrderViewModel Order { get; }
        public PaymentViewModel Payment { get; }
        public string PaymentReminderInfo { get; }
    }
}
