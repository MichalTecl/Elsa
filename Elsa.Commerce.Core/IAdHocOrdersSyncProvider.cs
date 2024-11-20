using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core
{
    public interface IAdHocOrdersSyncProvider
    {
        void SyncPaidOrders();
    }
}
