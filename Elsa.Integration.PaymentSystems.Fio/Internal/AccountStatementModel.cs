using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common.Utils;

namespace Elsa.Integration.PaymentSystems.Fio.Internal
{
    public class AccountStatementModel
    {
        public object Info { get; set; }

        public TransactionListModel TransactionList { get; set; }

        public IEnumerable<FioPayment> GetPayments()
        {
            foreach(var paymentModel in TransactionList.Transaction)
            {                
                yield return new FioPayment(paymentModel);
            }
        }

        public class FioPayment
        {
            private static readonly int[] s_excludeFromSearchText = new[] { 22, 0, 1, 3, 12, 14 };
            private const int c_idPohybu = 22;
            private const int c_datum = 0;
            private const int c_objem = 1;
            private const int c_mena = 14;
            private const int c_protiucet = 2;
            private const int c_kodBanky = 3;
            private const int c_nazevBanky = 12;
            private const int c_ks = 4;
            private const int c_vs = 5;
            private const int c_ss = 6;
            private const int c_zpravaProPrijemce = 16;



            public FioPayment(TransactionModel transaction)
            {
                TransactionId = transaction.GetValue<string>(c_idPohybu);
                PaymentDt = transaction.GetValue<DateTime>(c_datum);
                Value = transaction.GetValue<decimal>(c_objem);
                RemoteAccountNumber = transaction.GetValue(c_protiucet, true, "?");
                RemoteAccountBankCode = transaction.GetValue(c_kodBanky, true, "?");
                RemoteBankName = transaction.GetValue(c_nazevBanky, true, "?");
                VariableSymbol = transaction.GetValue(c_vs, true, "NEMA_VS");
                ConstantSymbol = transaction.GetValue(c_ks, true, string.Empty);
                SpecificSymbol = transaction.GetValue(c_ss, true, string.Empty);
                Message = transaction.GetValue(c_zpravaProPrijemce, true, string.Empty);
                Currency = transaction.GetValue<string>(c_mena);

                SenderName = StringUtil.Nvl(
                    transaction.GetValue(10, true, string.Empty),
                    transaction.GetValue(7, true, string.Empty),
                    transaction.GetValue(25, true, string.Empty));
                
                SearchText = StringUtil.NormalizeSearchText(
                    255,
                    transaction.Values.Where(v => v != null && !s_excludeFromSearchText.Contains(v.Id)).Select(v => v.Value?.Trim().ToLowerInvariant() ?? string.Empty).Distinct());
            }

            

            public string TransactionId { get; }
            public DateTime PaymentDt { get; }
            public decimal Value { get; }
            public string RemoteAccountNumber { get; }
            public string RemoteAccountBankCode { get; }
            public string RemoteBankName { get; }
            public string ConstantSymbol { get; }
            public string SpecificSymbol { get; }
            public string VariableSymbol { get; }
            public string Message { get; }
            public string Currency { get; }
            public string SearchText { get; }

            public string SenderName { get; }
        }
    }
}
