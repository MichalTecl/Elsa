using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.VirtualProducts.Model
{
    public class MaterialEditRequestModel
    {
        public int? MaterialId { get; set; }

        public string MaterialName { get; set; }

        public string NominalAmountText { get; set; }

        public int MaterialInventoryId { get; set; }

        public bool AutomaticBatches { get; set; }

        public bool RequiresInvoice { get; set; }

        public bool RequiresPrice { get; set; }

        public bool RequiresProductionPrice { get; set; }

        public bool HasThreshold { get; set; }
        public string ThresholdText { get; set; }

        public bool RequiresSupplierReference { get; set; }

        public bool Autofinalization { get; set; }

        public bool CanBeDigital { get; set; }

        public int? DaysBeforeWarnForUnused { get; set; }

        public string UnusedWarnMaterialType { get; set; }

        public bool UsageProlongsLifetime { get; set; }

        public bool NotAbandonedUntilNewerBatchUsed { get; set; }

        public bool UniqueBatchNumbers { get; set; }

        public string Comment { get; set; }

        public int? OrderFulfillDays { get; set; }

        public int? ExpirationMonths { get; set; }
        public int? DistributorExpirationLimit { get; set; }
        public int? RetailExpirationLimit { get; set; }
    }
}
