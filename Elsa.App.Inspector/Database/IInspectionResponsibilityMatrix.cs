using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.App.Inspector.Database
{
    [Entity]
    public interface IInspectionResponsibilityMatrix : IIntIdEntity
    {
        int InspectionTypeId { get; set; }
        IInspectionType InspectionType { get; }

        int ResponsibleUserId { get; set; }
        IUser ResponsibleUser { get; }

        [NVarchar(200, true)]
        string EMailOverride { get; set; }

        int DaysAfterDetect { get; set; }
    }
}
