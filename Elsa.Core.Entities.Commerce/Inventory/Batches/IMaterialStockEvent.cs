using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Inventory.Batches
{
    [Entity]
    public interface IMaterialStockEvent : IProjectRelatedEntity
    {
        int Id { get; }

        int BatchId { get; set; }
        IMaterialBatch Batch { get; }

        int UnitId { get; set; }
        IMaterialUnit Unit { get; }

        decimal Delta { get; set; }
    }
}
