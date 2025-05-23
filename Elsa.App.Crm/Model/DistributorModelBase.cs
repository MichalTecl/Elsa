using Elsa.Common.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public abstract class DistributorModelBase
    {
        private readonly IntCsvList _tagTypeIds = new IntCsvList();
        private readonly IntCsvList _customerGroupTypeIds = new IntCsvList();
        private readonly IntCsvList _salesRepIds = new IntCsvList();

        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        [JsonIgnore]
        public string TagTypesCsv { get => _tagTypeIds.Csv; set => _tagTypeIds.Csv = value; }

        [JsonIgnore]
        public string CustomerGroupTypesCsv { get => _customerGroupTypeIds.Csv; set => _customerGroupTypeIds.Csv = value; }

        [JsonIgnore]
        public string SalesRepIdsCsv { get => _salesRepIds.Csv; set => _salesRepIds.Csv = value; }

        public List<int> TagTypeIds => _tagTypeIds;
        public List<int> CustomerGroupTypeIds => _customerGroupTypeIds;

        public List<int> SalesRepIds => _salesRepIds;

        public List<CustomerTagAssignmentInfo> TagAssignments { get; } = new List<CustomerTagAssignmentInfo>();
    }
}
