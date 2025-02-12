using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.OrdersPacking.Entities
{
    [Entity]
    public interface IOrderReviewResult : IIntIdEntity, IHasAuthor
    {
        long OrderId { get; set; }
        IPurchaseOrder Order { get; }

        DateTime ReviewDt { get; set; }

        [NVarchar(100, true)]
        string Note { get; set; }
    }
}
