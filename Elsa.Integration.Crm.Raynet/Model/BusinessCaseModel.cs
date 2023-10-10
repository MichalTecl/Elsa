using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Integration.Crm.Raynet.Model
{
    public class BusinessCaseModel
    {
        public string Name { get; set; }

        public IdContainer Company { get; set; }

        public decimal TotalAmount { get; set; }

        public string ValidFrom { get; set; }

        public List<BcItemModel> Items { get; } = new List<BcItemModel>();
        public string Status { get; set; }
        public IdContainer BusinessCasePhase { get; set; }
    }
}
