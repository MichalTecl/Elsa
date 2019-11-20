using System.Collections.Generic;
using System.Text;

using Elsa.Commerce.Core.Units;
using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Commerce.Core.VirtualProducts.Model
{
    public interface IExtendedMaterialModel 
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

        bool RequiresInvoice { get; }

        bool RequiresSupplierReference { get; }
    }
}
