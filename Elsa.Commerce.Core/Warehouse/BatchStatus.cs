using Elsa.Core.Entities.Commerce.Inventory.Batches;

namespace Elsa.Commerce.Core.Warehouse
{
    public class BatchStatus
    {
        public static BatchStatus InProduction = new BatchStatus("production", "Připravuje se");

        public static BatchStatus Open = new BatchStatus("open", "Připravena");

        public static BatchStatus Closed = new BatchStatus("closed", "Spotřebována");

        public static BatchStatus Locked = new BatchStatus("locked", "Uzamčena");

        public BatchStatus(string status, string text)
        {
            Status = status;
            Text = text;
        }

        public string Status { get; }

        public string Text { get; }

        public static BatchStatus Get(IMaterialBatch batch)
        {
            if (batch.LockDt != null)
            {
                return BatchStatus.Locked;
            }

            if (!batch.IsAvailable)
            {
                return InProduction;
            }

            if (batch.CloseDt != null)
            {
                return Closed;
            }

            return Open;
        }
    }
}
