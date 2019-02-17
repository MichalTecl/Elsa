
using System.Collections.Generic;
using System.Linq;

using Elsa.Common;

namespace Elsa.Commerce.Core.Model
{
    public class BatchEventAmountSuggestions
    {
        public BatchEventAmountSuggestions(int batchId, int eventTypeId, decimal maximum, bool onlyIntegerValues)
        {
            BatchId = batchId;
            EventTypeId = eventTypeId;
            Maximum = maximum;
            OnlyIntegerValues = onlyIntegerValues;
        }

        public int BatchId { get; }
        public int EventTypeId { get; }
        public decimal Maximum { get; }
        public bool OnlyIntegerValues { get; }

        public List<BatchEventAmount> Suggestions { get; } = new List<BatchEventAmount>();

        public void AddSuggestion(Amount amount, string text = null)
        {
            if ((amount.Value > Maximum) || Suggestions.Any(s => s.Amount == amount.Value) || (amount.Value < 0.0001m))
            {
                return;
            }

            Suggestions.Add(new BatchEventAmount()
            {
                Amount = amount.Value,
                AmountText = amount.ToString(),
                AmountName = text ?? amount.ToString()
            });
        }

        public class BatchEventAmount
        {
            public string AmountText { get; set; }

            public string AmountName { get; set; }

            public decimal Amount { get; set; }
        }
    }
}
