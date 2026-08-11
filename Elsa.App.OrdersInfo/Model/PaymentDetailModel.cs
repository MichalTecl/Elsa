using System;

namespace Elsa.App.OrdersInfo.Model
{
    public class PaymentDetailModel
    {
        public string PaymentMethodName { get; set; }

        public bool IsPayOnDelivery { get; set; }

        public decimal TaxedPaymentCost { get; set; }

        public string OrderCurrencySymbol { get; set; }

        public string ComgateUrl { get; set; }

        public bool HasPairingInfo { get; set; }

        public string PaymentPairingUser { get; set; }

        public DateTime? PaymentPairingDt { get; set; }

        public bool HasPayment { get; set; }

        public PaymentInfoModel Payment { get; set; }
    }
}
