using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.VirtualProducts.Model
{
    public class MaterialSetupInfo
    {
        public int? MaterialId { get; set; }

        public string PreferredUnitSymbol { get; set; }

        public string AutoBatchNr { get; set; }

        public bool IsManufactured { get; set; }

        public string MaterialName { get; set; }

        public bool RequiresPrice { get; set; }

        public bool RequiresInvoice { get; set; }

        public bool AutomaticBatches { get; set; }

        public bool RequiresSupplierReference { get; set; }

        public bool Autofinalization { get; set; }
    }
}
