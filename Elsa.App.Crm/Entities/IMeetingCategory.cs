using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Entities
{
    [Entity]
    public interface IMeetingCategory : IIntIdEntity, IProjectRelatedEntity, IHasAuthor
    {
        [NVarchar(100, false)]
        string Title { get; set; }

        [NVarchar(32, false)]
        string ColorHex { get; set; }

        [NVarchar(32, true)]
        string IconClass { get; set; }

        int InitialStatusId { get; set; }
        IMeetingStatusType InitialStatus { get; }
    }
}
