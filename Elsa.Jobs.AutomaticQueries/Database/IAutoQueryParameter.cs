using System;
using System.Collections.Generic;
using System.Text;
using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Jobs.AutomaticQueries.Database
{
    [Entity]
    public interface IAutoQueryParameter : IIntIdEntity
    {
        int QueryId { get; set; }
        IAutomaticQuery Query { get; }

        [NVarchar(64, false)]
        string ParameterName { get; set; }

        [NVarchar(300, false)]
        string Expression { get; set; }

        bool TriggerOnly { get; set; }
    }
}
