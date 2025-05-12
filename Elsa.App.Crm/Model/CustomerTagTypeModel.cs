using Elsa.App.Crm.Entities;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public class CustomerTagTypeModel 
    {
        public int Id { get; set; }
        public string Name { get; set; }        
        public string CssClass { get; set; }        

        public bool IsRoot { get; set; }

        public int? FirstRootId { get; set; }

        public List<int> TransitionsTo { get; } = new List<int>();
        public List<int> TransitonsFrom { get; } = new List<int>();
        public List<int> AllTransitionParents { get; } = new List<int>();
    }
}
