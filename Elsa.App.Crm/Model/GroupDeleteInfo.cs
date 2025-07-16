using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public class GroupDeleteInfo
    {
        public bool CanDelete { get; set; }
        public bool NeedsConfirmation { get; set; }

        public string Message { get; set; }
    
    }
}
