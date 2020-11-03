using System;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.App.Inspector.Database
{
    [Entity]
    public interface IInspectionSession : IIntIdEntity, IProjectRelatedEntity
    {
        DateTime StartDt { get; set; }

        DateTime? EndDt { get; set; }

        [NVarchar(300, false)]
        string CurrentProcName { get; set; }
    }
}
