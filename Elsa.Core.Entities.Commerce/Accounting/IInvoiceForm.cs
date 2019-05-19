using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Accounting
{
    [Entity]
    public interface IInvoiceForm : IProjectRelatedEntity
    {
        int Id { get; }

        [NVarchar(100, true)]
        string InvoiceNumber { get; set; }

        [NVarchar(100, true)]
        string InvoiceVarSymbol { get; set; }

        [NVarchar(100, false)]
        string InvoiceFormNumber { get; set; }

        int FormTypeId { get; set; }
        IInvoiceFormType FormType { get; }

        DateTime IssueDate { get; set; }

        int? CancelUserId { get; set; }
        IUser CancelUser { get; }

        DateTime? CancelDt { get; set; }

        [NVarchar(1000, true)]
        string CancelReason { get; set; }

        int? SupplierId { get; set; }
        ISupplier Supplier { get; }

        IEnumerable<IInvoiceFormItem> Items { get; }
    }
}
