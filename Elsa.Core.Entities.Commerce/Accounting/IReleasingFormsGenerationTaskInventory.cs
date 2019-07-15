using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Inventory;
using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Accounting
{
    [Entity]
    public interface IReleasingFormsGenerationTaskInventory : IIntIdEntity
    {
        int GenerationTaskId { get; set; }
        IReleasingFormsGenerationTask GenerationTask { get; }

        int MaterialInventoryId { get; set; }
        IMaterialInventory MaterialInventory { get; }
    }
}
