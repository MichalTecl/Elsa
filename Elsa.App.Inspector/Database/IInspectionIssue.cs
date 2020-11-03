using System;
using System.Collections.Generic;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.App.Inspector.Database
{
    [Entity]
    public interface IInspectionIssue : IIntIdEntity, IProjectRelatedEntity
    {
        DateTime? PostponedTill { get; set; }

        int? PostponeUserId { get; set; }
        IUser PostponeUser { get; }

        int InspectionTypeId { get; set; }
        IInspectionType InspectionType { get; }

        [NVarchar(200, false)]
        string IssueCode { get; set; }

        [NVarchar(1000, false)]
        string Message { get; set; }

        DateTime FirstDetectDt { get; set; }

        DateTime LastDetectDt { get; set; }

        int LastSessionId { get; set; }
        IInspectionSession LastSession { get; }

        DateTime? ResolveDt { get; set; }

        [NVarchar(200, false)]
        string ProcName { get; set; }

        [ForeignKey(nameof(IInspectionIssueActionMenu.IssueId))]
        IEnumerable<IInspectionIssueActionMenu> Actions { get; }

        [ForeignKey(nameof(IInspectionIssueData.IssueId))]
        IEnumerable<IInspectionIssueData> Data { get; }
    }
}
