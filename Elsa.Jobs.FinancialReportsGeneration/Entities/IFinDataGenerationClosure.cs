using System;
using System.Collections.Generic;
using System.Text;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Jobs.FinancialReportsGeneration.Entities
{
    [Entity]
    public interface IFinDataGenerationClosure : IIntIdEntity, IProjectRelatedEntity
    {
        int Year { get; set; }
        int Month { get; set; }
        DateTime CloseDt { get; set; }

        [NVarchar(500, true)]
        string PackagePath { get; set; }

        DateTime? NotificationDt { get; set; }

        [NVarchar(100, false)]
        string PublicUid { get; set; }
    }
}
