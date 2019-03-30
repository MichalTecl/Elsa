using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Core;
using Elsa.Core.Entities.Commerce.Inventory.ProductionSteps;

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
        
        [ForeignKey(nameof(IMaterialComposition.CompositionId))]
        IEnumerable<IMaterialComposition> Composition { get; }

        [ForeignKey(nameof(IVirtualProductMaterial.ComponentId))]
        IEnumerable<IVirtualProductMaterial> VirtualProductMaterials { get; }

        int InventoryId { get; set; }
        IMaterialInventory Inventory { get; }

        bool AutomaticBatches { get; set; }

        bool? RequiresInvoiceNr { get; set; }

        bool? RequiresPrice { get; set; }

        bool? RequiresSupplierReference { get; set; }
        
        IEnumerable<IMaterialProductionStep> Steps { get; }
        
        IEnumerable<IMaterialThreshold> Thresholds { get; }
    }
}
