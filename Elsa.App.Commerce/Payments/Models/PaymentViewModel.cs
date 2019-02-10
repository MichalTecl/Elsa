using System.Linq;

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
            Amount = $"{((double)source.Value)} {source.Currency.Symbol}";
            VariableSymbol = GetSymbols(source.VariableSymbol, source.SpecificSymbol, source.ConstantSymbol);
            Message = source.Message;
            Sender = source.SenderName;
        }

        public long PaymentId { get; set; }
        public string SourceName { get; set; }
        public string PaymentDate { get; set; }
        public string Amount { get; set; }
        public string VariableSymbol { get; set; }
        public string Message { get; set; }
        public string Sender { get; set; }

        private static string GetSymbols(params string[] symbol)
        {
            var symbols =
                symbol.Where(s => !string.IsNullOrWhiteSpace(s) && (s != "NEMA_VS") && !IsOnlyZeroes(s))
                    .Select(s => s.Trim().ToLowerInvariant())
                    .Distinct();

            var x = string.Join(", ", symbols);

            if (string.IsNullOrWhiteSpace(x))
            {
                x = "-";
            }

            return x;
        }

        private static bool IsOnlyZeroes(string inp)
        {
            return inp.All(c => c == '0');
        }
    }
}
