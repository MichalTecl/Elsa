using System;
using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Common;

namespace Elsa.Integration.PaymentSystems.Common
{
    public interface IPaymentSystemHub
    {
        IEnumerable<IPayment> GetPayments(DateTime from, DateTime to);
    }
}
