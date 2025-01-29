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
            _source = source ?? throw new ArgumentNullException(nameof(source));

            Prevalidate();
        }

        private static string ToDtStr(params DateTime?[] d) => d.FirstOrDefault(i => i != null)?.ToString();

        public int ErpSystemId { get; set; }

        public string ErpOrderId => _source.id;

        public string OrderNumber => _source.order_num;

        public string DueDate => ToDtStr(GetLatestPreinvoice()?.due_date, _source.pur_date);

        public string PreInvId => GetLatestPreinvoice()?.id ?? "?";

        public string Price => _source.sum?.value.ToString();

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
                        throw new Exception($"Unexpected cutomer record type '{_source.customer.__typename}'");
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
                       _source.items == null ? throw new ArgumentException($"Order {OrderNumber} has no lineitems") :  _source.items.Select(i => new ApiLineItemModel(i)) ;

        public string DiscountsText
        {
            get
            {
                var elements = OrderPriceElements.Where(p => p.TypeErpName.Equals("discount")).Select(p => p.Title).ToList();
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
                throw new Exception($"Order {_source} has {nameof(_source.price_elements)}=null");

            var pe = _source.price_elements.Where(e => e.type == type).ToList();
            if (pe.Count != 1)
                throw new Exception($"Order {_source} unexpected count of price elements of type='{type}'. Expected=1 Found={pe.Count}");

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
            return _source.delivery_address ?? _source.invoice_address ?? throw new ArgumentException("No address");
        }

        public void Prevalidate()
        {
            try
            {
                if (_source.sum == null) throw new ArgumentNullException(nameof(_source.sum));
                if (_source.status == null) throw new ArgumentNullException(nameof(_source.status));
                if (_source.customer == null) throw new ArgumentNullException(nameof(_source.customer));
                if (_source.price_elements == null) throw new ArgumentNullException(nameof(_source.price_elements));
                if (_source.items == null) throw new ArgumentNullException(nameof(_source.items));


                CheckNotNull(OrderNumber, nameof(OrderNumber));
                CheckNotNull(DueDate, nameof(DueDate));                
                CheckNotNull(BuyDate, nameof(BuyDate));
                CheckNotNull(Price, nameof(Price));
                CheckNotNull(PriceWithVat, nameof(PriceWithVat));
                CheckNotNull(PurchaseDate, nameof(PurchaseDate));
                CheckNotNull(VarSymb, nameof(VarSymb));                
                CheckNotNull(CurrencyCode, nameof(CurrencyCode));
                CheckNotNull(ErpStatusName, nameof(ErpStatusName));
                CheckNotNull(ErpShippingName, nameof(ErpShippingName));
                CheckNotNull(ErpPaymentName, nameof(ErpPaymentName));
                CheckNotNull(Customer, nameof(Customer));
                CheckNotNull(Email, nameof(Email));
                CheckNotNull(ErpStatus, nameof(ErpStatus));                
                CheckNotNull(CustomerErpUid, nameof(CustomerErpUid));                
                CheckNotNull(OrderHash, nameof(OrderHash));

                foreach(var li in LineItems)
                {
                    CheckNotNull(li.ErpOrderItemId, "lineItem.ErpOrderItemId");
                    CheckNotNull(li.ProductName, "lineItem.ProductName");
                    
                    CheckNotNull(li.ErpProductId, "lineItem.ErpProductId");
                    CheckNotNull(li.TaxedPrice, "lineItem.TaxedPrice");
                    CheckNotNull(li.PriceWithoutTax, "lineItem.PriceWithoutTax");                    
                    CheckNotNull(li.ProductItemWeight, "lineItem.ProductItemWeight");                    

                    if (li.Quantity < 1)
                        throw new ArgumentException($"lineItem.Quentity <= 0");

                    if (li.TaxPercent <= 0)
                        throw new ArgumentException($"lineItem.TaxPercent <= 0");
                }

                foreach(var pe in OrderPriceElements)
                {
                    CheckNotNull(pe.Title, "priceElement.Title");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Order {OrderNumber}: {ex.Message}", ex);
            }
        }

        private static void CheckNotNull(string v, string fieldName, bool allowEmpty = false)
        {
            if (v == null || (!allowEmpty && string.IsNullOrEmpty(v)))
                throw new ArgumentNullException(fieldName);


        }
    }
}
