using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Commerce.Core.Warehouse;

namespace Elsa.Apps.ProductionService.Service.Process.Steps
{
    internal class CheckSourceSegmentEditability : IProductionRequestProcessingStep
    {
        private readonly IMaterialBatchRepository m_batchRepository;

        public CheckSourceSegmentEditability(IMaterialBatchRepository batchRepository)
        {
            m_batchRepository = batchRepository;
        }

        public void Process(ProductionRequestContext context)
        {
            if (context.Request.SourceSegmentId == null)
            {
                return;
            }

            //TODO check invoicing
        }
    }
}
