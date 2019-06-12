using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Accounting
{
    [Entity]
    public interface IInvoiceFormGenerationLog : IIntIdEntity
    {
        DateTime EventDt { get; set; }
        
        int InvoiceFormCollectionId { get; set; }
        IInvoiceFormCollection InvoiceFormCollection { get; }

        bool IsError { get; set; }

        bool IsWarning { get; set; }

        [NVarchar(4000, false)]
        string Message { get; set; }

        int? ApproveUserId { get; set; }
        IUser ApproveUser { get; }

        DateTime? ApproveDt { get; set; }
    }
}
