using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Common;

namespace Elsa.App.Commerce.Payments.Models
{
    public class PaymentViewModel
    {
        public PaymentViewModel(IPayment source)
        {
            PaymentId = source.Id;
            SourceName = source.PaymentSource?.Description;
            PaymentDate = DateUtil.FormatDateWithAgo(source.PaymentDt);
            Amount = source.Value;
            Currency = source.Currency.Symbol;
            VariableSymbol = (string.IsNullOrWhiteSpace(source.VariableSymbol) ? string.Empty : $" VS: {source.VariableSymbol}")
                           + (string.IsNullOrWhiteSpace(source.ConstantSymbol) ? string.Empty : $" KS: {source.VariableSymbol}")
                           + (string.IsNullOrWhiteSpace(source.SpecificSymbol) ? string.Empty : $" KS: {source.SpecificSymbol}");
            Message = source.Message;

        }

        public long PaymentId { get; set; }
        public string SourceName { get; set; }
        public string PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string VariableSymbol { get; set; }
        public string Message { get; set; }
        public string Detail { get; set; }
    }
}
