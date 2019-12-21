using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common.Utils;

namespace Elsa.Apps.ProductionService.Service.Process.Steps
{
    internal class ApplySourceSegmentId : IProductionRequestProcessingStep
    {
        private readonly IMaterialBatchRepository m_batchRepository;

        public ApplySourceSegmentId(IMaterialBatchRepository batchRepository)
        {
            m_batchRepository = batchRepository;
        }

        public void Process(ProductionRequestContext context)
        {
            if (context.Request.SourceSegmentId == null)
            {
                return;
            }

            var request = context.Request;

            var sourceSegment = m_batchRepository.GetBatchById(context.Request.SourceSegmentId.Value).Ensure();

            request.RecipeId = sourceSegment.Batch.RecipeId.Ensure("Šarže nevznikla z existující receptury");
            request.ProducingBatchNumber = request.ProducingBatchNumber ?? sourceSegment.Batch.BatchNumber;

            if (request.ProducingAmount == null)
            {
                request.ProducingAmount = sourceSegment.Batch.Volume;
            }
            
            request.ProducingPrice = sourceSegment.Batch.ProductionWorkPrice;
        }
    }
}
