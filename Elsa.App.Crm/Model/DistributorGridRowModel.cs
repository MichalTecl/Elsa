using Elsa.Common.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public class DistributorGridRowModel : DistributorModelBase
    {     
        public DateTime? LastContactDt { get; set; }
        public DateTime? FutureContactDt { get; set; }
        public int TotalOrdersCount { get; set; }
        public decimal TotalOrdersTaxedPrice { get; set; }       
        [JsonIgnore]
        public string SearchTag { get; set; }
        public List<SalesTrendTick> TrendModel { get; set; }
    }
}
