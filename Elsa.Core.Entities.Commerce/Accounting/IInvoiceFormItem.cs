using System.Collections.Generic;
using Elsa.Core.Entities.Commerce.Accounting.InvoiceFormItemBridges;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Accounting
{
    [Entity]
    public interface IInvoiceFormItem
    {
        int Id { get; }

        int InvoiceFormId { get; set; }

        IInvoiceForm InvoiceForm { get; }

        [NVarchar(300, false)]
        string MaterialName { get; set; }

        decimal Quantity { get; set; }

        int UnitId { get; set; }
        IMaterialUnit Unit { get; }

        decimal PrimaryCurrencyPrice { get; set; }

        decimal? SourceCurrencyPrice { get; set; }

        int? SourceCurrencyId { get; set; }
        ICurrency SourceCurrency { get; }

        int? ConversionId { get; set; }
        ICurrencyConversion Conversion { get; }

        int ItemLogicalNumber { get; set; }

        IEnumerable<IInvoiceFormItemMaterialBatch> Batches { get; }

        [NVarchar(NVarchar.Max, true)]
        string Note { get; set; }
    }
}
