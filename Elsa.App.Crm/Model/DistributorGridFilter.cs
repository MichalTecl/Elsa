using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public class DistributorGridFilter
    {
        public string TextFilter { get; set; }

        public List<int> Tags { get; set; }

        public int? SalesRepresentativeId { get; set; }

        public int? CustomerGroupTypeId { get; set; }

        public bool IncludeDisabled { get; set; }
    }
}
