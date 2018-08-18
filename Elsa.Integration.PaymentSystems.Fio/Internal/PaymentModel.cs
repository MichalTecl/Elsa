using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Integration;

namespace Elsa.Integration.PaymentSystems.Fio.Internal
{
    internal class PaymentModel : IPayment
    {
        public PaymentModel(AccountStatementModel.FioPayment source, ICurrencyRepository currencyRepository)
        {
            TransactionId = source.TransactionId;
            PaymentDt = source.PaymentDt;
            Value = source.Value;
            RemoteAccountNumber = source.RemoteAccountNumber;
            RemoteAccountBankCode = source.RemoteAccountBankCode;
            RemoteBankName = source.RemoteBankName;
            ConstantSymbol = source.ConstantSymbol;
            SpecificSymbol = source.SpecificSymbol;
            VariableSymbol = source.VariableSymbol;
            Message = source.Message;
            CurrencyId = currencyRepository.GetCurrency(source.Currency).Id;
        }

        public long Id => 0;

        public int ProjectId { get; set; }

        public int PaymentSourceId { get; set; }

        public IPaymentSource PaymentSource => null;

        public IProject Project => null;

        public string TransactionId { get; set; }

        public DateTime PaymentDt { get; set; }

        public decimal Value { get; set; }

        public int CurrencyId { get; set; }

        public ICurrency Currency => null;

        public string RemoteAccountNumber { get; set; }

        public string RemoteAccountBankCode { get; set; }

        public string RemoteBankName { get; set; }

        public string ConstantSymbol { get; set; }

        public string SpecificSymbol { get; set; }

        public string VariableSymbol { get; set; }

        public string Message { get; set; }

        public string SearchText { get; set; }
    }
}
