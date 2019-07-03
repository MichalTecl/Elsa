using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Accounting
{
    [Entity]
    public interface IInvoiceFormCollection : IIntIdEntity, IProjectRelatedEntity
    {
        [NVarchar(200, false)]
        string Name { get; set; }

        int GenerateUserId { get; set; }
        IUser GenerateUser { get; }

        int? ApproveUserId { get; set; }
        IUser ApproveUser { get; }

        DateTime? ApproveDt { get; set; }

        IEnumerable<IInvoiceForm> Forms { get; }
        
        IEnumerable<IInvoiceFormGenerationLog> Log { get; }

        int InvoiceFormTypeId { get; set; }
        IInvoiceFormType InvoiceFormType { get; }

        int Year { get; set; }

        int Month { get; set; }
    }
}
