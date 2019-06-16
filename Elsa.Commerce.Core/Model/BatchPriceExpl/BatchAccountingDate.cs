using System;

namespace Elsa.Commerce.Core.Model.BatchPriceExpl
{
    public class BatchAccountingDate
    {
        public BatchAccountingDate(DateTime dt):this(dt, true, null) { }

        public BatchAccountingDate(DateTime accountingDate, bool isFinal, string notFinalReason)
        {
            AccountingDate = accountingDate;
            IsFinal = isFinal;
            NotFinalReason = notFinalReason;
        }

        public DateTime AccountingDate { get; }

        public bool IsFinal { get; }

        public string NotFinalReason { get; }

        public override string ToString()
        {
            return $"{AccountingDate.Month.ToString().PadLeft(2, '0')}/{AccountingDate.Year}";
        }
    }
}
