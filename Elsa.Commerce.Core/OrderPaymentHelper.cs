using Elsa.Core.Entities.Commerce.Commerce;
using System;

namespace Elsa.Commerce.Core
{
    public class OrderPaymentHelper : IOrderPaymentHelper
    {
        public PaymentMethodType GetPaymentMethodType(IPurchaseOrder order)
        {
            if (string.IsNullOrWhiteSpace(order?.PaymentMethodName))
            {
                throw new ArgumentException("Objednávka nemá vyplněný název platební metody.", nameof(order));
            }

            var paymentMethodName = order.PaymentMethodName.Trim();

            if (paymentMethodName.IndexOf("dobírka", StringComparison.OrdinalIgnoreCase) >= 0)
                return PaymentMethodType.PayOnDelivery;

            if (string.Equals(paymentMethodName, "Bankovní převod", StringComparison.OrdinalIgnoreCase)
                || string.Equals(paymentMethodName, "Bankovním převodem", StringComparison.OrdinalIgnoreCase))
                return PaymentMethodType.BankTransfer;

            if (paymentMethodName.IndexOf("faktura", StringComparison.OrdinalIgnoreCase) >= 0)
                return PaymentMethodType.Invoice;

            if (paymentMethodName.IndexOf("kartou", StringComparison.OrdinalIgnoreCase) >= 0)
                return PaymentMethodType.Card;

            if (paymentMethodName.IndexOf("paypal", StringComparison.OrdinalIgnoreCase) >= 0)
                return PaymentMethodType.PayPal;

            throw new ArgumentException($"Nelze určit typ platební metody podle názvu \"{paymentMethodName}\".", nameof(order));
        }
    }
}
