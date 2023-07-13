using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Elsa.Commerce.Core.Model;

using Newtonsoft.Json;

namespace Elsa.Integration.Erp.Fler.Model
{
    public class FlerErpOrder : IErpOrderModel
    {
        private readonly RootObject m_order;

        private readonly List<FlerErpOrderItemModel> m_items;

        public FlerErpOrder(RootObject order, int erpSystemId)
        {
            m_order = order;
            ErpSystemId = erpSystemId;

            m_items = order.items.Select(i => new FlerErpOrderItemModel(i)).ToList();
        }

        public string ErpShippingName
        {
            get
            {
                var dm = m_order.info.delivery_method_18_info;
                if (dm != null)
                {
                    return $"{m_order.info.delivery_method_label.ToUpper()} - ({dm.id}) {dm.title}";
                }

                return m_order.info.delivery_method_label;
            }
        }

        public int ErpSystemId { get; set; }

        public string ErpOrderId => m_order.order.id;

        public string OrderNumber => m_order.order.id;

        public string DueDate => m_order.info.date_sent;

        public string PreInvId => m_order.order.invoice_num;

        public string Price => (m_order.order.sum_items + m_order.order.sum_postage).ToString();

        public string PriceWithVat => Price;

        public string PurchaseDate => m_order.order.date_created;

        public string BuyDate => m_order.order.conversion_date;

        public string VarSymb => OrderNumber;

        public string InvoiceCompanyName => m_order.address_billing?.company;
        public string InvoiceFirstName => GetComponents(m_order.address_billing?.name, 2).FirstOrDefault();
        public string InvoiceSurname => GetComponents(m_order.address_billing?.name, 2).LastOrDefault();
        public string InvoiceStreet => m_order.address_billing?.address;
        public string InvoiceDescriptiveNumber => null;
        public string InvoiceOrientationNumber => null;
        public string InvoiceCity => m_order.address_billing?.city;
        public string InvoiceZip => m_order.address_billing?.zip;
        public string InvoiceCountry => m_order.address_billing?.country;
        public string InvoicePhone => m_order.info.customer_mobile_number_with_country_prefix;

        public string DeliveryCompanyName => m_order.address_delivery?.company;
        public string DeliveryName => GetComponents(m_order.address_delivery?.name, 2).FirstOrDefault();
        public string DeliverySurname => GetComponents(m_order.address_delivery?.name, 2).LastOrDefault();
        public string DeliveryStreet => m_order.address_delivery?.address;
        public string DeliveryDescriptiveNumber => null;
        public string DeliveryOrientationNumber => null;
        public string DeliveryCity => m_order.address_delivery?.city;
        public string DeliveryZip => m_order.address_delivery?.zip;
        public string DeliveryCountry => m_order.address_delivery?.country;
        public string DeliveryPhone => m_order.info.customer_mobile_number_with_country_prefix;

        public string CurrencyCode => m_order.order.currency;

        public string ErpStatusName => m_order.order.state;

        public string ErpPaymentName => m_order.info.payment_method_label;

        public string Customer => (m_order.address_billing?.name) ?? (m_order.address_delivery?.name) ?? m_order.order.buyer_username;

        public string Email => "nemame@zfleru.maily";

        public string UserId => m_order.order.buyer_username;

        public string PayDate => m_order.info.date_marked_paid;

        public string Paid => m_order.info.date_marked_paid;

        public string ErpStatus => m_order.order.state;

        public string NoteLeftByCustomer => null;

        public string InternalNote => null;

        public string InvoiceSent => m_order.info.date_sent;

        public string InvoiceId => m_order.order.invoice_num;

        public string PreviewText => $"{OrderNumber} {InvoiceFirstName} {InvoiceSurname} {InternalNote}";

        public string DeliveryFormattedHouseNumber => m_order.address_delivery.address;
        
        public string FormattedHouseNumber => m_order.address_delivery.address;

        public bool IsPayOnDelivery => m_order.info.payment_is_upfront != 1;
        
        public IEnumerable<IErpPriceElementModel> OrderPriceElements
        {
            get
            {
                yield break;
            }
        }

        public IEnumerable<IErpOrderItemModel> LineItems => m_items;

        public string DiscountsText { get; set; }

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

        public string CustomerErpUid => throw new NotImplementedException();

        public void SetDebugNumber(string nnum)
        {
            throw new NotImplementedException();
        }

        private static IEnumerable<string> GetComponents(string inp, int count)
        {
            int generated = 0;

            var parts = (inp?.Trim().Split(' ') ?? new string[0]).Select(i => i.Trim());

            foreach (var part in parts)
            {
                yield return part;
                generated++;
            }

            while (generated < count)
            {
                yield return string.Empty;
                generated++;
            }
        }
    }
}
