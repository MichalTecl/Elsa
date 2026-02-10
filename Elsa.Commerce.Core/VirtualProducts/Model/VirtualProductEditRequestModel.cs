using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.VirtualProducts.Model
{
    public class VirtualProductEditRequestModel
    {
        public int? VirtualProductId { get; set; }

        public string UnhashedName { get; set; }

        public List<VpMaterialEditRequestModel> Materials { get; set; }


        public class VpMaterialEditRequestModel
        {
            public string DisplayText { get; set; }
        }
    }
}
