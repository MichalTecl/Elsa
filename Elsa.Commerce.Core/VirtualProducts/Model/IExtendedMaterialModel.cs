using System.Collections.Generic;
using System.Text;

using Elsa.Commerce.Core.Units;
using Elsa.Common.EntityComments;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Inventory;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Commerce.Core.VirtualProducts.Model
{
    public interface IExtendedMaterialModel : ISingleCommentEntity
    {
        int Id { get; }

        string Name { get; }

        IMaterialUnit NominalUnit { get; }

        decimal NominalAmount { get; }
        
        IMaterialUnit BatchUnit { get; }

        decimal BatchAmount { get; }

        IMaterial Adaptee { get; }
        
        bool HasThreshold { get; }

        string ThresholdText { get; }

        IExtendedMaterialModel CreateBatch(decimal batchAmount, IMaterialUnit preferredBatchUnit, IUnitConversionHelper conversions);
        
        void Print(StringBuilder target, string depthLevelTrim);

        int InventoryId { get; }

        string InventoryName { get; }

        bool IsManufactured { get; }

        bool CanBeConnectedToTag { get; }

        bool AutomaticBatches { get; }

        bool RequiresPrice { get; }

        bool RequiresProductionPrice { get; }

        bool RequiresInvoice { get; }

        bool RequiresSupplierReference { get; }

        bool Autofinalization { get; }
        bool CanBeDigital { get;  }

        int? DaysBeforeWarnForUnused { get; }
                
        string UnusedWarnMaterialType { get; }

        bool UsageProlongsLifetime { get; }

        bool NotAbandonedUntilNewerBatchUsed { get; }

        bool UniqueBatchNumbers { get; }

        bool IsHidden { get; }

        int? OrderFulfillDays { get; }

        int? ExpirationMonths { get; }
        int? DistributorExpirationLimit { get; }
        int? RetailExpirationLimit { get; }
    }
}
