using Elsa.Common;

namespace Elsa.Commerce.Core.SaleEvents
{
    public class SaleEventAllocationDto
    {
        public SaleEventAllocationDto(int materialId, string batchNumber, Amount allocatedQuantity, Amount returnedQuantity)
        {
            MaterialId = materialId;
            BatchNumber = batchNumber;
            AllocatedQuantity = allocatedQuantity;
            ReturnedQuantity = returnedQuantity;
        }
        
        public int MaterialId { get; }

        public string BatchNumber { get; }

        public Amount AllocatedQuantity { get; }
        
        public Amount ReturnedQuantity { get; }
    }
}
