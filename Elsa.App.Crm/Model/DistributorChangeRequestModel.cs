using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public class DistributorChangeRequestModel
    {
        public int CustomerId { get; set; }

        public bool? HasStore { get; set; }
        public bool? HasEshop { get; set; }

        public List<int> AddedTags { get; set; }
        public List<int> RemovedTags { get; set; }
        
        public List<DistributorAddressViewModel> ChangedAddresses { get; set; }
    }
}
