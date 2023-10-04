using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Apps.ProductionPlanner.Entities
{
    [Entity]
    public interface IMonthlySalesMultiplier : IIntIdEntity, IProjectRelatedEntity
    {
        DateTime ForecastMonth { get; set; }                
        int MultiplierPercent { get; set; }
        bool IsSystemGenerated { get; set; }
        int InsertUserId { get; set; }
        IUser InsertUser { get; }
        DateTime InsertDt { get; set; }

        [NVarchar(1000, false)]
        string Note { get; set; }
    }
}
