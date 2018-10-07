using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Model;

namespace Elsa.Integration.Erp.Elerp.Model
{
    public class ElerpPriceElement : IErpPriceElementModel
    {
        public string ErpPriceElementId { get; set; }

        public string TypeErpName { get; set; }

        public string Title { get; set; }

        public string Value { get; set; }

        public string TaxPercent { get; set; }
    }
}
