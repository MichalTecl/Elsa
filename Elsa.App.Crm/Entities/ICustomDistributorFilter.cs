using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Entities
{
    [Entity]
    public interface ICustomDistributorFilter : IIntIdEntity, IHasAuthor
    {
        DateTime Created { get; set; }

        [NVarchar(100, false)]
        string Name { get; set; }

        [NVarchar(NVarchar.Max, false)]
        string JsonData { get; set; }
    }
}
