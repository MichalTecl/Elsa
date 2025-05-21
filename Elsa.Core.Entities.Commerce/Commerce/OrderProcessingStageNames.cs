using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Core.Entities.Commerce.Commerce
{
    public static class OrderProcessingStageNames
    {
        public const string Packing = "Packing";
        public const string BatchesAssignment = "Batch_assignment_change";
    }
}
