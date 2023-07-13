using Robowire.RobOrm.Core;
using System;

namespace Elsa.Core.Entities.Commerce.Commerce
{
    [Entity]
    public interface IPurchaseOrderHistory : IPurchaseOrder
    {
        DateTime? AuditDate { get; set; }
    }
}
