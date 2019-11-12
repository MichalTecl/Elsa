using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.ProductionService.Service.Process
{
    internal interface IProductionRequestProcessingStep
    {
        void Process(ProductionRequestContext context);
    }
}
