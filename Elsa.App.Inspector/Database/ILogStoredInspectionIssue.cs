using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;

namespace Elsa.App.Inspector.Database
{
    [Entity]
    public interface ILogStoredInspectionIssue : IIntIdEntity, IProjectRelatedEntity
    {
        [NVarchar(200, false)]
        string IssueTypeName { get; set; }

        [NVarchar(200, false)]
        string IssueCode { get; set; }

        DateTime LogDt { get; set; }

        DateTime? InspectorProcessedDt { get; set; }

        [NVarchar(1000, false)]
        string Message { get; set; }
    }
}
