using System;

namespace Elsa.App.OrdersInfo.Model
{
    public class PaymentInfoModel
    {
        public long PaymentId { get; set; }

        public string SourceName { get; set; }

        public DateTime PaymentDt { get; set; }

        public decimal Amount { get; set; }

        public string CurrencySymbol { get; set; }

        public string VariableSymbol { get; set; }

        public string Message { get; set; }

        public string SenderName { get; set; }
    }
}
