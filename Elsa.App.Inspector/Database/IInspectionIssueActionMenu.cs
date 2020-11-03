using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.App.Inspector.Database
{
    [Entity]
    public interface IInspectionIssueActionMenu : IIntIdEntity
    {
        int IssueId { get; set; }
        IInspectionIssue Issue { get; }

        [NVarchar(300, false)]
        string ActionName { get; set; }

        [NVarchar(300, false)]
        string ControlUrl { get; set; }
    }
}
