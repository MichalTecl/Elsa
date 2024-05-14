using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.ProductionService.Service.Process.Steps
{
    internal class CheckUniqueBatchNumber : IProductionRequestProcessingStep
    {
        private readonly IMaterialBatchRepository _batchRepository;

        public CheckUniqueBatchNumber(IMaterialBatchRepository batchRepository)
        {
            _batchRepository = batchRepository;
        }

        public void Process(ProductionRequestContext context)
        {
            if (string.IsNullOrEmpty(context.Request.ProducingBatchNumber) || (context.TargetMaterial.UniqueBatchNumbers != true))
                return;

            var existing = _batchRepository.GetBatchByNumber(context.TargetMaterial.Id, context.Request.ProducingBatchNumber);
            if (existing?.Batch?.Id != context.Request.SourceSegmentId)
            {
                context.InvalidateRequest($"Není povoleno opakované použití čísla šarže");
            }
        }
    }
}
