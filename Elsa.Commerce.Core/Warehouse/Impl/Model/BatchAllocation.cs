using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Commerce.Core.Model;
using Elsa.Common;

namespace Elsa.Commerce.Core.Warehouse.Impl.Model
{
    public class BatchAllocation
    {
        public BatchAllocation(string batchNumber, Amount totalBatchNumberAvailable, Amount allocated, List<Tuple<int, Amount>> batchIdAllocations, DateTime batchCreated)
        {
            BatchNumber = batchNumber;
            TotalBatchNumberAvailable = totalBatchNumberAvailable;
            Allocated = allocated;
            BatchIdAllocations = batchIdAllocations;
            BatchCreated = batchCreated;
        }

        public string BatchNumber { get; } 

        public Amount TotalBatchNumberAvailable { get; }

        public Amount Allocated { get; }

        public DateTime BatchCreated { get; }

        public List<Tuple<int, Amount>> BatchIdAllocations { get; }
    }

}
