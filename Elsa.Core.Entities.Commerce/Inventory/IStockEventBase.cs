using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Core;

namespace Elsa.Core.Entities.Commerce.Inventory
{
    public interface IStockEventBase 
    {
        int MaterialId { get; set; }
        IMaterial Material { get; }

        int UnitId { get; set; }
        IMaterialUnit Unit { get; }

        decimal Volume { get; set; }
    }
}
