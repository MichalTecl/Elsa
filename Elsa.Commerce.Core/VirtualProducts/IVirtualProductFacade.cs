using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.VirtualProducts
{
    public interface IVirtualProductFacade
    {
        void ProcessVirtualProductEditRequest(int? virtualProductId, string name, string[] materialEntries);
    }
}
