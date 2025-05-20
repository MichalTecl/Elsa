using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public class BulkTaggingRequest
    {
        public int TagTypeId { get; set; }
        public string Note { get; set; }
        public List<int> CustomerIds { get; set; }
        public DistributorGridFilter Filter { get; set; }

        public bool Set { get; set; }
    }
}
