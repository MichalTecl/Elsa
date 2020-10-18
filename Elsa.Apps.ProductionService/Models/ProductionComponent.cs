using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Commerce.Core;
using Elsa.Common;

namespace Elsa.Apps.ProductionService.Models
{
    public class ProductionComponent
    {
        public int MaterialId { get; set; }

        public string MaterialName { get; set; }

        public string UnitSymbol { get; set; }

        public decimal RequiredAmount { get; set; }

        public bool IsValid { get; set; }
        
        public List<RequestValidationMessage> Messages { get; } = new List<RequestValidationMessage>();

        public List<ProductionComponentResolution> Resolutions { get; } = new List<ProductionComponentResolution>();
        public int SortOrder { get; set; }
        public string LastClientAmount { get; set; }
        public bool HasBatchChangeWarning { get; set; }

        public void Invalidate(string message)
        {
            IsValid = false;

            Messages.Add(new RequestValidationMessage(true, message));
        }
    }
}
