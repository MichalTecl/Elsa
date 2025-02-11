using Elsa.Common.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public class DistributorGridRowModel
    {
        private readonly IntCsvList _tagTypeIds = new IntCsvList();
        private readonly IntCsvList _customerGroupTypeIds = new IntCsvList();
        private readonly IntCsvList _salesRepIds = new IntCsvList();

        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        [JsonIgnore]
        public string TagTypesCsv { get; set; }

        [JsonIgnore]
        public string CustomerGroupTypesCsv { get; set; }

        [JsonIgnore]
        public string SalesRepIdsCsv { get; set; }

        public DateTime? LastContactDt { get; set; }
        public DateTime? FutureContactDt { get; set; }
        public int TotalOrdersCount { get; set; }
        public decimal TotalOrdersTaxedPrice { get; set; }
        public decimal PrevQuarterOrdersPrice { get; set; }
        public decimal LastQuarterOrdersPrice { get; set; }
        public decimal CrossQuarterTrend { get; set; }

        [JsonIgnore]
        public string SearchTag { get; set; }

        public List<int> TagTypeIds => _tagTypeIds;
        public List<int> CustomerGroupTypeIds => _customerGroupTypeIds;

        public List<int> SalesRepIds => _salesRepIds;
        
    }
}
