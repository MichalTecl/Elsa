using System;

using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Commerce.Core
{
    public interface IProductRepository
    {
        void PreloadCache(int erpId);

        IProduct GetProduct(int erpId, string erpProductId, DateTime orderDt, string productName);
    }
}
