using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.App.Inspector.Database
{
    [Entity]
    public interface IInspectionIssueActionsHistory : IIntIdEntity
    {
        int InspectionIssueId { get; set; }
        IInspectionIssue InspectionIssue { get; }

        int UserId { get; set; }
        IUser User { get; }

        DateTime ActionDt { get; set; }

        [NVarchar(300, false)]
        string ActionText { get; set; }
    }
}
