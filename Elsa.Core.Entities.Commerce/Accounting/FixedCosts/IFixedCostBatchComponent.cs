using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Accounting.FixedCosts
{
    [Entity]
    public interface IFixedCostBatchComponent:IIntIdEntity
    {
        int CalculationId { get; set; }
        IFixedCostMonthCalculation Calculation { get; }

        int BatchId { get; set; }
        IMaterialBatch Batch { get; }

        decimal Value { get; set; }

        decimal Multiplier { get; set; }
    }
}
