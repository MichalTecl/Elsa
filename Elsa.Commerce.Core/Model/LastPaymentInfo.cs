using System;

namespace Elsa.Commerce.Core.Model
{
    public sealed class LastPaymentInfo
    {
        public LastPaymentInfo(int paymentSourceId, DateTime lastPaymentDt)
        {
            PaymentSourceId = paymentSourceId;
            LastPaymentDt = lastPaymentDt;
        }

        public int PaymentSourceId { get; }

        public DateTime LastPaymentDt { get; }
    }
}
