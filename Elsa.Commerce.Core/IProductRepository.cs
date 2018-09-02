using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Commerce.Core
{
    public interface IProductRepository
    {
        void PreloadCache(int erpId);

        IProduct GetProduct(int erpId, string erpProductId, DateTime orderDt, string productName);
    }
}
