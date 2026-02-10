using System;
using System.Collections.Generic;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Core;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Inventory
{
    [Entity]
    public interface IMaterial : IProjectRelatedEntity
    {
        int Id { get; }

        [NVarchar(256, false)]
        string Name { get; set; }

        int NominalUnitId { get; set; }
        IMaterialUnit NominalUnit { get; }

        decimal NominalAmount { get; set; }
        
        [ForeignKey(nameof(IVirtualProductMaterial.ComponentId))]
        IEnumerable<IVirtualProductMaterial> VirtualProductMaterials { get; }

        int InventoryId { get; set; }
        IMaterialInventory Inventory { get; }

        bool AutomaticBatches { get; set; }

        bool? RequiresInvoiceNr { get; set; }

        bool? RequiresPrice { get; set; }

        bool? RequiresProductionPrice { get; set; }

        bool? RequiresSupplierReference { get; set; }
        IEnumerable<IMaterialThreshold> Thresholds { get; }

        bool? UseAutofinalization { get; set; }

        bool? CanBeDigitalOnly { get; set; }

        int? DaysBeforeWarnForUnused { get; set; }

        [NVarchar(256, true)]
        string UnusedWarnMaterialType { get; set; }

        bool? UsageProlongsLifetime { get; set; }

        bool? NotAbandonedUntilNewerBatchUsed { get; set; }

        bool? UniqueBatchNumbers { get; set; }

        DateTime? HideDt { get; set; }
        int? HideUserId { get; set; }
        IUser HideUser { get; }

        int? OrderFulfillDays { get; set; }

        int? ExpirationMonths { get; set; }
        int? DistributorExpirationLimit { get; set; }
        int? RetailExpirationLimit { get; set; }
    }
}
