using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Common.SystemCounters;
using Elsa.Core.Entities.Commerce.Core;
using Elsa.Core.Entities.Commerce.Inventory;

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

        [NVarchar(100, true)]
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

        int MaterialInventoryId { get; set; }
        IMaterialInventory MaterialInventory { get; }

        [NVarchar(200, true)]
        string SecondaryInventory { get; set; }

        IEnumerable<IInvoiceFormItem> Items { get; }

        int InvoiceFormCollectionId { get; set; }
        IInvoiceFormCollection InvoiceFormCollection { get; }

        [NVarchar(0, true)]
        string PriceCalculationLog { get; set; }

        bool? PriceHasWarning { get; set; }

        [NVarchar(350, true)]
        string Text { get; set; }

        [NVarchar(1000, true)]
        string Explanation { get; set; }

        int? SourceTaskId { get; set; }
        IReleasingFormsGenerationTask SourceTask { get; }

        int? CounterId { get; set; }
        ISystemCounter Counter { get; }
    }
}
