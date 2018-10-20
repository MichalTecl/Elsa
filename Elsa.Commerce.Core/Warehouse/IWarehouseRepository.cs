using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Commerce.Core.Warehouse
{
    public interface IWarehouseRepository
    {
        IMaterialStockEvent AddMaterialStockEvent(IMaterial material, decimal delta, IMaterialUnit unitId, string note);
    }
}
