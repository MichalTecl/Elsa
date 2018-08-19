using System;

namespace Elsa.Integration.PaymentSystems.Common
{
    public interface IPaymentSystemCommonSettings
    {
        DateTime HistoryStart { get; }

        int BatchSizeDays { get; }
    }
}
