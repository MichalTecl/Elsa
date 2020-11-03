using System.Collections.Generic;
using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.App.Inspector.Database
{
    [Entity]
    public interface IInspectionIssueData : IIntIdEntity
    {
        int IssueId { get; set; }
        IInspectionIssue Issue { get; }

        [NVarchar(100, false)]
        string PropertyName { get; set; }

        [NVarchar(1000, true)]
        string StrValue { get; set; }
        
        int? IntValue { get; set; }

        bool? IsArray { get; set; }
    }
}
