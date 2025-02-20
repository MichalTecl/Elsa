﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Model;

using Newtonsoft.Json;

namespace Elsa.Integration.Erp.Flox.Protocol.OrderModel
{
    public class FloxErpOrderModel : IErpOrderModel
    {
        private string m_source;
        private bool? m_dobirkaEx;

        [XmlIgnore]
        public int ErpSystemId { get; set; }
        
        [XmlElement("order_id")]
        public string ErpOrderId { get; set; }

        [XmlElement("order_num")]
        public string OrderNumber { get; set; }

        [XmlElement("due_date")]
        public string DueDate { get; set; }
        
        [XmlIgnore]
        public int OrderNumParsed
        {
            get
            {
                int parsed;
                if(!int.TryParse(OrderNumber, out parsed))
                {
                    return -1;
                }
                return parsed;
            }
        }

        [XmlElement("pre_inv_id")]
        public string PreInvId { get; set; }

        [XmlElement("price")]
        public string Price { get; set; }

        [XmlElement("price_with_vat")]
        public string PriceWithVat { get; set; }

        [XmlElement("pur_date")]
        public string PurchaseDate { get; set; }

        [XmlElement("buy_date")]
        public string BuyDate { get; set; }

        [XmlElement("var_symb")]
        public string VarSymb { get; set; }

        [XmlElement("address_company_name ")]
        public string InvoiceCompanyName { get; set; }

        [XmlElement("address_name")]
        public string InvoiceFirstName { get; set; }

        [XmlElement("address_surname")]
        public string InvoiceSurname { get; set; }

        [XmlElement("address_street")]
        public string InvoiceStreet { get; set; }

        [XmlElement("address_descriptive_number")]
        public string InvoiceDescriptiveNumber { get; set; }

        [XmlElement("address_orientation_number")]
        public string InvoiceOrientationNumber { get; set; }

        [XmlElement("address_city")]
        public string InvoiceCity { get; set; }

        [XmlElement("address_zip")]
        public string InvoiceZip { get; set; }

        [XmlElement("address_country ")]
        public string InvoiceCountry { get; set; }

        [XmlElement("address_phone")]
        public string InvoicePhone { get; set; }

        [XmlElement("delivery_address_company_name")]
        public string DeliveryCompanyName { get; set; }

        [XmlElement("delivery_address_name")]
        public string DeliveryName { get; set; }

        [XmlElement("delivery_address_surname")]
        public string DeliverySurname { get; set; }

        [XmlElement("delivery_address_street")]
        public string DeliveryStreet { get; set; }

        [XmlElement("delivery_address_descriptive_number")]
        public string DeliveryDescriptiveNumber { get; set; }

        [XmlElement("delivery_address_orientation_number")]
        public string DeliveryOrientationNumber { get; set; }

        [XmlElement("delivery_address_city")]
        public string DeliveryCity { get; set; }

        [XmlElement("delivery_address_zip")]
        public string DeliveryZip { get; set; }

        [XmlElement("delivery_address_country")]
        public string DeliveryCountry { get; set; }

        [XmlElement("delivery_address_phone")]
        public string DeliveryPhone { get; set; }

        [XmlElement("currency_code")]
        public string CurrencyCode { get; set; }

        [XmlElement("status_name")]
        public string ErpStatusName { get; set; }

        [XmlElement("shipping_name")]
        public string ErpShippingName { get; set; }

        [XmlElement("payment_name")]
        public string ErpPaymentName { get; set; }

        [XmlElement("customer")]
        public string Customer { get; set; }

        [XmlElement("u_email")]
        public string Email { get; set; }        

        [XmlElement("pay_date ")]
        public string PayDate { get; set; }

        [XmlElement("paid ")]
        public string Paid { get; set; }

        [XmlElement("status")]
        public string ErpStatus { get; set; }

        [XmlElement("note")]
        public string NoteLeftByCustomer { get; set; }

        [XmlElement("internal_note")]
        public string InternalNote { get; set; }

        [XmlElement("price_elements")]
        public PriceElements PriceElements { get; set; }

        [XmlElement("order_items")]
        public OrderItems OrderItems { get; set; }

        [XmlElement("invoice_sent")]
        public string InvoiceSent { get; set; }

        [XmlElement("inv_id")]
        public string InvoiceId { get; set; }

        [XmlElement("user_id")]
        public string CustomerUserId { get; set; }

        [XmlElement("cid")]
        public string CustomerCompanyId { get; set; }

        [XmlIgnore]
        public string CustomerErpUid
        {
            get
            {
                return CustomerUidCalculator.GetCustomerUid(CustomerCompanyId, CustomerUserId, Email);
            }
        }

        [XmlIgnore]
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

        public string GetElementValue(string elementType)
        {
            var elm = PriceElements.Items.FirstOrDefault(i => i.TypeErpName.Equals(elementType, StringComparison.InvariantCultureIgnoreCase));

            if (string.IsNullOrWhiteSpace(elm?.Title))
            {
                throw new Exception("Chybny format - nenalezena informace \"" + elementType + "\"");
            }

            return elm.Title;
        }
        
        public bool IsPayOnDelivery
        {
            get
            {
                if (m_dobirkaEx != null)
                {
                    return m_dobirkaEx.Value;
                }

                var paymentText = GetElementValue("payment");

                return paymentText.ToLowerInvariant().Contains("dobírka");
             }
        }

        public IEnumerable<IErpPriceElementModel> OrderPriceElements => PriceElements.Items;

        public IEnumerable<IErpOrderItemModel> LineItems => OrderItems.Items;

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

        public void SetDebugNumber(string nnum)
        {
            OrderNumber = nnum;
            InvoiceId = nnum;
            PreInvId = nnum;
            VarSymb = nnum;
        }

        [XmlIgnore]
        public string Source
        {
            get { return m_source ?? "Flox"; }
            set { m_source = value; }
        }

        [XmlElement("u_vat_id")]
        public string VatId { get; set; }

        [XmlElement("u_company_id")]
        public string CompanyRegistrationId { get; set; }

        [XmlElement("last_change")]
        public string ErpLastChangeDt { get; set; }

        public override string ToString()
        {
            return
                $"{InvoiceFirstName ?? DeliveryName ?? InvoiceCompanyName ?? DeliveryCompanyName} {InvoiceSurname ?? DeliverySurname ?? InvoiceCompanyName ?? DeliveryCompanyName}; {OrderNumber}";
        }

                
        public void ValidateAddress()
        {
            if (HasNull(DeliverySurname))
            {
                DeliveryName = InvoiceFirstName;
                DeliverySurname = InvoiceSurname;
            }

            if (HasNull(DeliveryName))
            {
                DeliveryName = DeliverySurname;
            }
            
            if (HasNull(DeliverySurname))
            {
                throw new Exception("Chybi prijmeni");
            }
                                    
            if (HasNull(InvoiceStreet, InvoiceCity, InvoiceZip, FormattedHouseNumber) && HasNull(DeliveryStreet, DeliveryCity, DeliveryZip, DeliveryFormattedHouseNumber))
            {
                throw new Exception("Ani fakturacni ani dorucovaci adresa nejsou kopletni");
            }

        }

        public void OverrideDobirka(bool value)
        {
            m_dobirkaEx = value;
        }

        private static bool HasNull(params string[] p)
        {
            if (p == null)
            {
                return true;
            }

            foreach(var i in p)
            {
                if (string.IsNullOrWhiteSpace(i))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
