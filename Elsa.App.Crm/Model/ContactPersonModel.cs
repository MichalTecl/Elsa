using Elsa.Core.Entities.Commerce.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public class ContactPersonModel 
    {
        public int Id { get; set; }
        public bool IsSystem { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }        
        public string Phone { get; set; }
        public string Note { get; set; }        
    }
}
