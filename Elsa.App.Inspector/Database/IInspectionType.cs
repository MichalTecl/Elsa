using System;
using System.Collections;
using System.Collections.Generic;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.App.Inspector.Database
{
    [Entity]
    public interface IInspectionType : IIntIdEntity, IProjectRelatedEntity
    {
        [NVarchar(200, false)]
        string Name { get; set; }

        DateTime LastRun { get; set; }

        int LastSessionId { get; set; }
        IInspectionSession LastSession { get; }        
    }
}
