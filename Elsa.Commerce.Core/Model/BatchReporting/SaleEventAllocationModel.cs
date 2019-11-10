using System;
using Elsa.Commerce.Core.Units;
using Elsa.Common;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce.SaleEvents;

namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class SaleEventAllocationModel
    {
        private Amount m_blockedAmount;
        private Amount m_returnedAmount;
        private Amount m_usedAmount;
        
        public int SaleEventId { get; private set; }

        internal DateTime SortDt { get; private set; }

        public string EventName { get; private set; }

        public string Date => StringUtil.FormatDate(SortDt);

        public string UsedAmount => m_usedAmount?.ToString();
        
        public string BlockedAmount => m_blockedAmount?.ToString();

        public string ReturnedAmount => m_returnedAmount?.ToString();

        public string StatusText => m_returnedAmount == null ? "Blokováno" : "Prodáno";

        public string Author { get; private set; }

        public bool Populate(ISaleEventAllocation entity, AmountProcessor amountProcessor)
        {
            if (SaleEventId != 0 && SaleEventId != entity.SaleEventId)
            {
                return false;
            }

            SaleEventId = entity.SaleEventId;
            
            if (string.IsNullOrWhiteSpace(EventName))
            {
                EventName = entity.SaleEvent.Name;
                SortDt = entity.ReturnDt ?? entity.AllocationDt;
                Author = entity.AllocationUser?.EMail;
            }

            m_blockedAmount = amountProcessor.Add(m_blockedAmount, new Amount(entity.AllocatedQuantity, entity.Unit));
            m_returnedAmount = amountProcessor.Add(m_returnedAmount, new Amount(entity.ReturnedQuantity ?? 0m, entity.Unit));

            if (m_returnedAmount == null)
            {
                m_usedAmount = m_blockedAmount;
            }

            m_usedAmount = amountProcessor.Subtract(m_blockedAmount, m_returnedAmount);

            return true;
        }
    }
}
