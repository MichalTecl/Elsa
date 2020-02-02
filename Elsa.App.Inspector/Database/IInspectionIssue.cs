using System;
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
        [NVarchar(200, false)]
        string InspectionType { get; set; }

        [NVarchar(200, false)]
        string IssueCode { get; set; }

        [NVarchar(1000, false)]
        string Message { get; set; }

        DateTime Created { get; set; }

        DateTime? PostponeTo { get; set; }

        int? PostponeUserId { get; set; }
        IUser PostponeUser { get; }
    }
}
