using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public class DistributorFilterValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public int NumberOfRecords { get; set; }
        public string FilterText { get; set; }
    }
}
