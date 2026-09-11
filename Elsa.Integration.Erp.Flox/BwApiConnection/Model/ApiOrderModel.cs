using BwApiClient.Model.Data;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Model;
using Elsa.Integration.Erp.Flox.Protocol.OrderModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Integration.Erp.Flox.BwApiConnection.Model
{
    // TODO: DueDate, BuyDate, PurchaseDate
    public class ApiOrderModel : IErpOrderModel
    {
        private readonly Order _source;

        public ApiOrderModel(Order source)
        {
            _source = source ?? throw new InvalidOperationException("BW API nevrátilo data objednávky.");

            Prevalidate();
        }

        private static string ToDtStr(params DateTime?[] d) => d.FirstOrDefault(i => i != null)?.ToString();

        public int ErpSystemId { get; set; }

        public string ErpOrderId => _source.id;

        public string OrderNumber => _source.order_num;

        public string DueDate => ToDtStr(GetLatestPreinvoice()?.due_date, _source.pur_date);

        public string PreInvId => GetLatestPreinvoice()?.id ?? "?";

        public string Price => (_source.vat_summary ?? throw new InvalidOperationException("Chybí souhrn DPH objednávky (vat_summary).")).Sum(vs => vs.tax_base).ToString();

        public string PriceWithVat => _source.sum?.value.ToString();

        public string PurchaseDate => ToDtStr(_source.pur_date);

        public string BuyDate => ToDtStr(GetLatestInvoice()?.buy_date, _source.pur_date);

        public string VarSymb => _source.order_num?.Trim();

        public string InvoiceCompanyName => _source.invoice_address.company_name?.Trim();

        public string InvoiceFirstName => _source.invoice_address.name?.Trim();

        public string InvoiceSurname => _source.invoice_address.surname?.Trim();

        public string InvoiceStreet => _source.invoice_address.street?.Trim();

        public string InvoiceDescriptiveNumber => _source.invoice_address.descriptive_number?.Trim();

        public string InvoiceOrientationNumber => _source.invoice_address.orientation_number?.Trim();

        public string InvoiceCity => _source.invoice_address.city?.Trim();

        public string InvoiceZip => _source.invoice_address.zip?.Trim();

        public string InvoiceCountry => _source.invoice_address.country?.Trim();

        public string InvoicePhone => _source.invoice_address.phone?.Trim();

        public string DeliveryCompanyName => GetDeliveryAddress().company_name?.Trim();

        public string DeliveryName => GetDeliveryAddress().name?.Trim();

        public string DeliverySurname => GetDeliveryAddress().surname?.Trim();

        public string DeliveryStreet => GetDeliveryAddress().street?.Trim();

        public string DeliveryDescriptiveNumber => GetDeliveryAddress().descriptive_number?.Trim();

        public string DeliveryOrientationNumber => GetDeliveryAddress().orientation_number?.Trim();

        public string DeliveryCity => GetDeliveryAddress().city?.Trim();

        public string DeliveryZip => GetDeliveryAddress().zip?.Trim();

        public string DeliveryCountry => GetDeliveryAddress().country?.Trim();

        public string DeliveryPhone => GetDeliveryAddress().phone?.Trim();

        public string CurrencyCode => _source.sum.currency.code?.Trim();

        public string ErpStatusName => _source.status.name?.Trim();

        public string ErpShippingName => GetPriceElement("shipping").title?.Trim();

        public string ErpPaymentName => GetPriceElement("payment").title?.Trim();

        public string Customer
        {
            get
            {
                switch (_source.customer.__typename)
                {
                    case "UnauthenticatedEmail":
                        return $"{_source.customer.name} {_source.customer.surname}";
                    case "Person":
                        return $"{_source.customer.name} {_source.customer.surname}";
                    case "Company":
                        return _source.customer.company_name;
                    default:
                        throw new Exception($"Neznámý typ zákazníka '{_source.customer.__typename ?? "neuvedeno"}'.");
                }
            }
        }

        public string Email => _source.customer.email?.Trim();

        public string PayDate => null;

        public string Paid => null;

        public string ErpStatus => _source.status.id.ToString();

        public string NoteLeftByCustomer => _source.note;

        public string InternalNote => _source.internal_note;

        public string InvoiceSent => null;

        public string InvoiceId => null;

        public string PreviewText => $"{OrderNumber} {InvoiceFirstName} {InvoiceSurname} {InternalNote}";

        private string FormatHouseNumber(string orientation, string descriptive)
        {
            if (string.IsNullOrWhiteSpace(orientation))
            {
                return descriptive;
            }
            else if (string.IsNullOrWhiteSpace(descriptive))
            {
                return orientation;
            }
            else
            {
                return $"{descriptive}/{orientation}";
            }
        }

        public string DeliveryFormattedHouseNumber => FormatHouseNumber(DeliveryOrientationNumber, DeliveryDescriptiveNumber);

        public string FormattedHouseNumber => FormatHouseNumber(InvoiceOrientationNumber, InvoiceDescriptiveNumber);

        public bool IsPayOnDelivery => GetPriceElement("payment").title.ToLowerInvariant().Contains("dobírka");

        public string CustomerErpUid => CustomerUidCalculator.GetCustomerUid(_source.customer.companyid, _source.customer.personid, Email);

        public IEnumerable<IErpPriceElementModel> OrderPriceElements => _source.price_elements.Select(e => new ApiPriceElementModel(e));

        public IEnumerable<IErpOrderItemModel> LineItems =>
                       _source.items == null ? throw new ArgumentException("Objednávka nemá seznam položek (items).") :  _source.items.Select(i => new ApiLineItemModel(i)) ;

        private static readonly string[] _discountMarkers = { "discount", "gift", "percent_discount" };
        public string DiscountsText
        {
            get
            {
                var elements = OrderPriceElements
                    .Where(p => _discountMarkers.Contains(p.TypeErpName?.Trim() ?? string.Empty, StringComparer.InvariantCultureIgnoreCase))
                    .Select(p => p.Title?.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.InvariantCultureIgnoreCase)
                    .ToList();

                if (!elements.Any())
                {
                    return null;
                }

                return string.Join(" ", elements);
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        [JsonIgnore]
        public string OrderHash
        {
            get
            {
                var json = JsonConvert.SerializeObject(this);

                var oData = Encoding.UTF8.GetBytes(json);

                using (var md5 = new MD5CryptoServiceProvider())
                {
                    var hash = md5.ComputeHash(oData);
                    return Convert.ToBase64String(hash);
                }
            }
        }

        public string VatId => _source.customer.vat_id;

        public string CompanyRegistrationId => _source.customer.company_reg_id;

        public string ErpLastChangeDt => ToDtStr(_source.last_change);

        public void SetDebugNumber(string nnum)
        {
            throw new NotImplementedException();
        }

        private OrderPriceElement GetPriceElement(string type)
        {
            if (_source.price_elements == null)
                throw new Exception("Chybí seznam cenových položek objednávky (price_elements).");

            var pe = _source.price_elements.Where(e => e.type == type).ToList();
            if (pe.Count != 1)
                throw new Exception($"Objednávka musí obsahovat právě jednu cenovou položku typu '{GetPriceElementDescription(type)}', nalezeno: {pe.Count}.");

            return pe[0];
        }

        private PreinvoiceRef GetLatestPreinvoice()
        {
            return _source.preinvoices?.OrderByDescending(i => i.created)?.FirstOrDefault();
        }

        private InvoiceRef GetLatestInvoice()
        {
            return _source.invoices?.OrderByDescending(i => i.id)?.FirstOrDefault();
        }

        private AddressData GetDeliveryAddress()
        {
            return _source.delivery_address ?? _source.invoice_address ?? throw new ArgumentException("Objednávka nemá dodací ani fakturační adresu.");
        }

        public void Prevalidate()
        {
            try
            {
                CheckRequired(_source.sum, "celková cena objednávky (sum)");
                CheckRequired(_source.sum.currency, "měna objednávky (sum.currency)");
                CheckRequired(_source.status, "stav objednávky (status)");
                CheckRequired(_source.customer, "zákazník (customer)");
                CheckRequired(_source.invoice_address, "fakturační adresa (invoice_address)");
                CheckRequired(_source.price_elements, "seznam cenových položek (price_elements)");
                CheckRequired(_source.items, "seznam položek objednávky (items)");

                if (_source.vat_summary == null || _source.vat_summary.Count == 0)
                    throw new InvalidOperationException("Chybí souhrn DPH objednávky (vat_summary).");

                if (_source.vat_summary.Any(item => item == null))
                    throw new InvalidOperationException("Souhrn DPH objednávky (vat_summary) obsahuje prázdný záznam.");

                if (_source.items.Any(item => item == null))
                    throw new InvalidOperationException("Seznam položek objednávky (items) obsahuje prázdný záznam.");

                if (_source.price_elements.Any(item => item == null))
                    throw new InvalidOperationException("Seznam cenových položek objednávky (price_elements) obsahuje prázdný záznam.");

                CheckRequired(OrderNumber, "číslo objednávky");
                CheckRequired(DueDate, "datum splatnosti");
                CheckRequired(BuyDate, "datum nákupu");
                CheckRequired(Price, "cena bez DPH");
                CheckRequired(PriceWithVat, "cena s DPH");
                CheckRequired(PurchaseDate, "datum přijetí objednávky");
                CheckRequired(VarSymb, "variabilní symbol");
                CheckRequired(CurrencyCode, "kód měny");
                CheckRequired(ErpStatusName, "název stavu objednávky");
                CheckRequired(ErpShippingName, "způsob dopravy");
                CheckRequired(ErpPaymentName, "způsob platby");
                CheckRequired(Customer, "jméno zákazníka nebo název firmy");
                CheckRequired(Email, "e-mail zákazníka");
                CheckRequired(_source.status.id, "ID stavu objednávky");
                CheckRequired(ErpStatus, "ID stavu objednávky");
                CheckRequired(CustomerErpUid, "identifikátor zákazníka");
                CheckRequired(OrderHash, "kontrolní otisk objednávky");

                foreach(var li in LineItems)
                {
                    CheckRequired(li.ErpOrderItemId, "ID položky objednávky");
                    CheckRequired(li.ProductName, $"název položky objednávky {li.ErpOrderItemId}");
                    
                    CheckRequired(li.ErpProductId, $"ID produktu u položky {li.ErpOrderItemId}");
                    CheckRequired(li.TaxedPrice, $"cena s DPH u položky {li.ErpOrderItemId}");
                    CheckRequired(li.PriceWithoutTax, $"cena bez DPH u položky {li.ErpOrderItemId}");
                    CheckRequired(li.ProductItemWeight, $"hmotnost položky {li.ErpOrderItemId}");

                    if (li.Quantity < 1)
                        throw new ArgumentException($"Položka {li.ErpOrderItemId} má neplatné množství {li.Quantity}; množství musí být alespoň 1.");
                }

                foreach(var pe in OrderPriceElements)
                {
                    CheckRequired(pe.Title, $"název cenové položky {pe.ErpPriceElementId ?? "bez ID"}");
                }
            }
            catch (Exception ex)
            {
                var orderIdentification = string.IsNullOrEmpty(_source.order_num)
                    ? $"ERP ID {_source.id ?? "neuvedeno"}"
                    : _source.order_num;

                throw new Exception($"Objednávka {orderIdentification}: {ex.Message}", ex);
            }
        }

        private static void CheckRequired(object value, string description)
        {
            if (value == null)
                throw new InvalidOperationException($"Chybí {description}.");
        }

        private static void CheckRequired(string value, string description)
        {
            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException($"Chybí {description}.");
        }

        private static string GetPriceElementDescription(string type)
        {
            switch (type)
            {
                case "shipping":
                    return "doprava";
                case "payment":
                    return "platba";
                default:
                    return type;
            }
        }
    }
}
