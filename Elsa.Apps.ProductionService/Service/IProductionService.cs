using Elsa.Apps.ProductionService.Models;

namespace Elsa.Apps.ProductionService.Service
{
    public interface IProductionService
    {
        void ValidateRequest(ProductionRequest request);
        void ProcessRequest(ProductionRequest request);
    }
}
