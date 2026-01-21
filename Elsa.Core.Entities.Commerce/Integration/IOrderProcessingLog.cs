using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Core.Entities.Commerce.Integration
{
    [Entity]
    public interface IOrderProcessingLog : IIntIdEntity, IOrderRelatedEntity
    {
        [NVarchar(100, false)]
        string ProcessCode { get; set; }

        DateTime ProcessDt { get; set; }
    }
}
