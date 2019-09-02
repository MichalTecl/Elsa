using System.Collections.Generic;
using System.Linq;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.CurrencyRates;
using Elsa.Common.Data;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Accounting.InvoiceFormItemBridges;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Inventory;
using Robowire;

namespace Elsa.Invoicing.Core.Data.Adapters
{
    internal class InvoiceFormItemAdapter : AdapterBase<IInvoiceFormItem>, IInvoiceFormItem
    {
        public InvoiceFormItemAdapter(IServiceLocator serviceLocator, IInvoiceFormItem adaptee) : base(serviceLocator, adaptee)
        {
        }

        public int Id => Adaptee.Id;
        public int InvoiceFormId { get => Adaptee.InvoiceFormId; set => Adaptee.InvoiceFormId = value; }
        public string MaterialName { get => Adaptee.MaterialName; set => Adaptee.MaterialName = value; }
        public decimal Quantity { get => Adaptee.Quantity; set => Adaptee.Quantity = value; }
        public int UnitId { get => Adaptee.UnitId; set => Adaptee.UnitId = value; }
        public decimal PrimaryCurrencyPrice { get => Adaptee.PrimaryCurrencyPrice; set => Adaptee.PrimaryCurrencyPrice = value; }
        public decimal? SourceCurrencyPrice { get => Adaptee.SourceCurrencyPrice; set => Adaptee.SourceCurrencyPrice = value; }
        public int? SourceCurrencyId { get => Adaptee.SourceCurrencyId; set => Adaptee.SourceCurrencyId = value; }
        public int? ConversionId { get => Adaptee.ConversionId; set => Adaptee.ConversionId = value; }
        public int ItemLogicalNumber { get => Adaptee.ItemLogicalNumber; set => Adaptee.ItemLogicalNumber = value; }
        
        public string Note { get => Adaptee.Note; set => Adaptee.Note = value; }

        public IMaterialUnit Unit => Get<IUnitRepository, IMaterialUnit>("Unit", r => r.GetUnit(UnitId));

        public ICurrency SourceCurrency => Get<ICurrencyRepository, ICurrency>("SourceCurrency",
            r => SourceCurrencyId == null ? null : r.GetAllCurrencies().FirstOrDefault(c => c.Id == SourceCurrencyId));

        public ICurrencyConversion Conversion => Get<ICurrencyConversionHelper, ICurrencyConversion>("Conversion",
            r => ConversionId == null ? null : r.GetConversion(ConversionId.Value));

        public IEnumerable<IInvoiceFormItemMaterialBatch> Batches =>
            Get<IInvoiceFormsRepository, IEnumerable<IInvoiceFormItemMaterialBatch>>("Batches",
                r => r.GetFormItemBatchesByItemId(Id));

        public IInvoiceForm InvoiceForm { get; }

    }
}
