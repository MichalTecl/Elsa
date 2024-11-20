using System;

namespace Elsa.Integration.Erp.Flox
{
    public interface IErpCommonSettings
    {
        int MaxQueryDays { get; }

        DateTime HistoryStart { get; }

        int OrderSyncHistoryDays { get; }

        bool UseIncrementalOrderChangeMode { get; }

        int IncrementalModeMaxHistoryDays { get; }

        int PaidOrdersSyncHistoryDays { get; }
    }
}
