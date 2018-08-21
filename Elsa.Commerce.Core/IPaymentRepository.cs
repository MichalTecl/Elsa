using System;
using System.Collections.Generic;

using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Common;

namespace Elsa.Commerce.Core
{
    public interface IPaymentRepository
    {
        void PreloadCache(DateTime from, DateTime to);

        IPayment GetPayment(int paymentSourceId, string transactionId);

        void SavePayment(IPayment payment);

        IEnumerable<LastPaymentInfo> GetLastPaymentDates();

        IEnumerable<IPayment> GetPaymentsByVarSymb(string orderVarSymbol);

        IEnumerable<IPayment> GetPayments(DateTime min, DateTime max);

        IPayment GetPayment(long id);
    }
}
