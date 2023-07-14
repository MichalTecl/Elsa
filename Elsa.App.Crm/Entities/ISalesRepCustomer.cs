using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Crm;
using Robowire.RobOrm.Core;
using System;

namespace Elsa.App.Crm.Entities
{
    [Entity]
    public interface ISalesRepCustomer : IIntIdEntity
    {
        int SalesRepId { get; set; }
        ISalesRepresentative SalesRep { get; }

        int CustomerId { get; set; }
        ICustomer Customer { get; }

        DateTime ValidFrom { get; set; }
        DateTime? ValidTo { get; set; }

        int InsertUserId { get; set; }
        IUser InsertUser { get; }
    }
}
