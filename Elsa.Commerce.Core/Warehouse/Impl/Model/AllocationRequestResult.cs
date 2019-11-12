using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Common;

namespace Elsa.Commerce.Core.Warehouse.Impl.Model
{
    public class AllocationRequestResult
    {
        internal AllocationRequestResult(int materialId, bool completelyAllocated, Amount allocatedAmount, Amount remainingAmount)
        {
            MaterialId = materialId;
            CompletelyAllocated = completelyAllocated;
            AllocatedAmount = allocatedAmount;
            RemainingAmount = remainingAmount;
        }

        public int MaterialId { get; }

        public bool CompletelyAllocated { get; }

        public Amount AllocatedAmount { get; }

        public Amount RemainingAmount { get; }

        public IList<BatchAllocation> Allocations { get; } = new List<BatchAllocation>();
    }
}
