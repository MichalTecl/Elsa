using Elsa.Core.Entities.Commerce.Core;
using RobOrm.Core;
using RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Core.Entities.Commerce.Crm
{
    [Entity]
    public interface INewsletterSubscriber : IProjectRelatedEntity
    {
        int Id { get; }

        [NVarchar(200, false)]
        string SourceName { get; set; }

        [NVarchar(500, false)]
        string Email { get; set; }

        DateTime? SubscribeDt { get; set; }

        DateTime? UnsubscribeDt { get; set; }
    }
}
