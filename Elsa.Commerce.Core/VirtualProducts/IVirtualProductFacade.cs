using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Commerce.Core.VirtualProducts
{
    public interface IVirtualProductFacade
    {
        IVirtualProduct ProcessVirtualProductEditRequest(int? virtualProductId, string name, string[] materialEntries);
    }
}
