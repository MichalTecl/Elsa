using Elsa.Commerce.Core.Model.BatchPriceExpl;

namespace Elsa.Apps.InvoiceForms.Model
{
    public class InvoiceFormModelBase
    {
        public int InvoiceFormId { get; set; }

        public string IssueDate { get; set; }

        public string InvoiceFormNumber { get; set; }

        public decimal PrimaryCurrencyPriceWithoutVat { get; set; }

        public string FormattedPrimaryCurrencyPriceWithoutVat { get; set; }

        public PriceCalculationLog PriceCalculationLog { get; set; } = PriceCalculationLog.Empty;

        public string OriginalCurrencyPrice { get; set; }

        public string OriginalCurrencySymbol { get; set; }

        public string ConversionRateLink { get; set; }

        public string ConversionRate { get; set; }

        public string CancelReason { get; set; }

        public string InventoryName { get; set; }

        public string DownloadUrl { get; set; }
    }
}
