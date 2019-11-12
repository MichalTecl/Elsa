using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Apps.ProductionService.Models;

namespace Elsa.Apps.ProductionService.Service
{
    public interface IProductionService
    {
        void ValidateRequest(ProductionRequest request);
    }
}
