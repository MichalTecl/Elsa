using Elsa.Commerce.Core.Units;
using Elsa.Common;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Newtonsoft.Json;

namespace Elsa.Apps.Inventory.Model
{
    public class StockEventViewModel
    {
        private Amount m_amount;

        public StockEventViewModel(IMaterialStockEvent e)
        {
            EventId = e.Id;
            Author = e.User.EMail;
            Note = e.Note;
            GroupingKey = e.EventGroupingKey;
            m_amount = new Amount(e.Delta, e.Unit);
        }

        [JsonIgnore]
        public string GroupingKey { get; }

        public int EventId { get; }

        public string Amount => $"{StringUtil.FormatDecimal(m_amount.Value)} {m_amount.Unit.Symbol}";


        public string Author { get; }

        public string Note { get; }

        public void Join(StockEventViewModel rawEvent, AmountProcessor amountProcessor)
        {
            m_amount = amountProcessor.Add(m_amount, rawEvent.m_amount);
        }
    }
}
