using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Core.Entities.Commerce.Commerce
{
    public interface IOrderRelatedEntity
    {
        long PurchaseOrderId { get; set; }
        IPurchaseOrder PurchaseOrder { get; }
    }
}
