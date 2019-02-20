using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

namespace Elsa.Apps.Inventory.Model
{
    public class StockEventViewModel
    {
        public StockEventViewModel(IMaterialStockEvent e)
        {
            EventId = e.Id;
            Amount = $"{StringUtil.FormatDecimal(e.Delta)} {e.Unit.Symbol}";
            Author = e.User.EMail;
            Note = e.Note;
        }

        public int EventId { get; }

        public string Amount { get; }
        
        public string Author { get; }

        public string Note { get; }
    }
}
