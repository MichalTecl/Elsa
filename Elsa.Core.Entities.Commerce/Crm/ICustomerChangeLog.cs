using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Core.Entitites.Crm
{
    [Entity]
    public interface ICustomerChangeLog : IIntIdEntity, IHasAuthor, ICustomerRelatedEntity
    {
        DateTime ChangeDt { get; set; }

        [NVarchar(100, false)]
        string Field { get; set; }

        [NVarchar(255, true)]
        string OldValue { get; set; }

        [NVarchar(255, true)]
        string NewValue { get; set; }

        [NVarchar(255, false)]
        string GroupingKey { get; set; }
    }
}
