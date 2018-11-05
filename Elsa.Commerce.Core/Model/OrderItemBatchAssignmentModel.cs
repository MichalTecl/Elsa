using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.Model
{
    public class OrderItemBatchAssignmentModel
    {
        public long OrderItemId { get; set; }

        public int MaterialBatchId { get; set; }

        public decimal AssignedQuantity { get; set; }

        public string BatchNumber { get; set; }
    }
}
