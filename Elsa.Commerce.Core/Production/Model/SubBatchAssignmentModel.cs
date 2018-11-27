using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.Production.Model
{
    public class SubBatchAssignmentModel
    {
        public int? UsedBatchId { get; set; }

        public string UsedBatchNumber { get; set; }

        public decimal UsedAmount { get; set; }

        public string UsedAmountUnitSymbol { get; set; }

        public string AssignmentUid { get; } = Guid.NewGuid().ToString();
    }
}
