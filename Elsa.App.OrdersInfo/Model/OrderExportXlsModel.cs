using System;
using XlsSerializer.Core.Attributes;

namespace Elsa.App.OrdersInfo.Model
{
    [HeaderStyle(FontStyle = FontStyle.Bold)]
    public class OrderExportXlsModel
    {
        [XlsColumn("A", "ID objednávky", "0")]
        public long OrderId { get; set; }

        [XlsColumn("B", "Číslo objednávky", "@")]
        public string OrderNumber { get; set; }

        [XlsColumn("C", "ERP objednávka", "@")]
        public string ErpOrderId { get; set; }

        [XlsColumn("D", "ERP ID", "0")]
        public int? ErpId { get; set; }

        [XlsColumn("E", "ERP systém", "@")]
        public string ErpName { get; set; }

        [XlsColumn("F", "Datum nákupu", "dd.mm.yyyy hh:mm")]
        public DateTime PurchaseDate { get; set; }

        [XlsColumn("G", "Datum objednání", "dd.mm.yyyy hh:mm")]
        public DateTime BuyDate { get; set; }

        [XlsColumn("H", "Datum splatnosti", "dd.mm.yyyy")]
        public DateTime DueDate { get; set; }

        [XlsColumn("I", "ERP stav", "@")]
        public string ErpStatusName { get; set; }

        [XlsColumn("J", "Interní stav", "@")]
        public string OrderStatusName { get; set; }

        [XlsColumn("K", "Zákazník", "@")]
        public string CustomerName { get; set; }

        [XlsColumn("L", "ERP UID zákazníka", "@")]
        public string CustomerErpUid { get; set; }

        [XlsColumn("M", "E-mail zákazníka", "@")]
        public string CustomerEmail { get; set; }

        [XlsColumn("N", "IČO", "@")]
        public string CompanyRegId { get; set; }

        [XlsColumn("O", "DIČ", "@")]
        public string VatId { get; set; }

        [XlsColumn("P", "Fakturační adresa", "@")]
        public string InvoiceAddress { get; set; }

        [XlsColumn("Q", "Dodací adresa", "@")]
        public string DeliveryAddress { get; set; }

        [XlsColumn("R", "Telefon", "@")]
        public string DeliveryPhone { get; set; }

        [XlsColumn("S", "Cena bez DPH", "0.00")]
        public decimal Price { get; set; }

        [XlsColumn("T", "Cena s DPH", "0.00")]
        public decimal PriceWithVat { get; set; }

        [XlsColumn("U", "Měna", "@")]
        public string Currency { get; set; }

        [XlsColumn("V", "Variabilní symbol", "@")]
        public string VariableSymbol { get; set; }

        [XlsColumn("W", "Číslo faktury", "@")]
        public string InvoiceId { get; set; }

        [XlsColumn("X", "Číslo zálohové faktury", "@")]
        public string PreInvoiceId { get; set; }

        [XlsColumn("Y", "Platební metoda", "@")]
        public string PaymentMethodName { get; set; }

        [XlsColumn("Z", "Cena platební metody s DPH", "0.00")]
        public decimal TaxedPaymentCost { get; set; }

        [XlsColumn("AA", "Platbu spároval", "@")]
        public string PaymentPairingUser { get; set; }

        [XlsColumn("AB", "Datum spárování platby", "dd.mm.yyyy hh:mm")]
        public DateTime? PaymentPairingDt { get; set; }

        [XlsColumn("AC", "Zdroj platby", "@")]
        public string PaymentSource { get; set; }

        [XlsColumn("AD", "Datum platby", "dd.mm.yyyy hh:mm")]
        public DateTime? PaymentDt { get; set; }

        [XlsColumn("AE", "Částka platby", "0.00")]
        public decimal? PaymentAmount { get; set; }

        [XlsColumn("AF", "Měna platby", "@")]
        public string PaymentCurrency { get; set; }

        [XlsColumn("AG", "VS platby", "@")]
        public string PaymentVariableSymbol { get; set; }

        [XlsColumn("AH", "Odesílatel platby", "@")]
        public string PaymentSender { get; set; }

        [XlsColumn("AI", "Zpráva platby", "@")]
        public string PaymentMessage { get; set; }

        [XlsColumn("AJ", "Způsob dopravy", "@")]
        public string ShippingMethodName { get; set; }

        [XlsColumn("AK", "Cena dopravy s DPH", "0.00")]
        public decimal TaxedShippingCost { get; set; }

        [XlsColumn("AL", "Zabalil", "@")]
        public string PackingUser { get; set; }

        [XlsColumn("AM", "Datum zabalení", "dd.mm.yyyy hh:mm")]
        public DateTime? PackingDt { get; set; }

        [XlsColumn("AN", "Hmotnost položek v kg", "0.000")]
        public decimal TotalWeightKg { get; set; }

        [XlsColumn("AO", "Položky", "@")]
        public string Items { get; set; }

        [XlsColumn("AP", "Vypořádané šarže", "@")]
        public string BatchAssignments { get; set; }

        [XlsColumn("AQ", "Slevy/kupóny", "@")]
        public string Discounts { get; set; }

        [XlsColumn("AR", "Cenové elementy", "@")]
        public string PriceElements { get; set; }

        [XlsColumn("AS", "Poznámka zákazníka", "@")]
        public string CustomerNote { get; set; }

        [XlsColumn("AT", "Interní poznámka", "@")]
        public string InternalNote { get; set; }

        [XlsColumn("AU", "Záznam vytvořil", "@")]
        public string InsertUser { get; set; }

        [XlsColumn("AV", "Záznam vytvořen", "dd.mm.yyyy hh:mm")]
        public DateTime InsertDt { get; set; }

        [XlsColumn("AW", "Poslední změna ERP", "dd.mm.yyyy hh:mm")]
        public DateTime? ErpLastChange { get; set; }

        [XlsColumn("AX", "Datum vrácení", "dd.mm.yyyy hh:mm")]
        public DateTime? ReturnDt { get; set; }
    }
}
