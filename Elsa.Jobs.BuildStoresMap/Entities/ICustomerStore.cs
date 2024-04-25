using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Crm;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Jobs.BuildStoresMap.Entities
{
    [Entity]
    public interface ICustomerStore : IIntIdEntity
    {
        int CustomerId { get; set; }
        ICustomer Customer { get; }

        [NVarchar(300, false)]
        string Name { get; set; }

        [NVarchar(200, false)]
        string SystemRecordName { get; set; }

        [NVarchar(300, true)]
        string Address { get; set; }

        [NVarchar(150, true)]
        string City { get; set; }

        [NVarchar(300, true)]
        string Www { get; set; }

        [NVarchar(150, true)]
        string PreviewName { get; set; }

        [NVarchar(16, true)]
        string Lat { get; set; }

        [NVarchar(16, true)]
        string Lon { get; set; }
    }
}

