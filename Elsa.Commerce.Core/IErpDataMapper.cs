using System;

using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Commerce.Core
{
    public interface IErpDataMapper
    {
        IPurchaseOrder MapOrder(IErpOrderModel source, Action<IPurchaseOrder> onBeforeSave);
    }
}
