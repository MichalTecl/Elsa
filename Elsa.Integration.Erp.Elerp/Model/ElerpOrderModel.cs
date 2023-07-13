using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Model;

using Newtonsoft.Json;

namespace Elsa.Integration.Erp.Elerp.Model
{
    internal class ElerpOrderModel : IErpOrderModel
    {
        public int ErpSystemId { get; set; }

        public string ErpOrderId { get; set; }

        public string OrderNumber { get; set; }

        public string DueDate { get; set; }

        public string PreInvId { get; set; }

        public string Price { get; set; }

        public string PriceWithVat { get; set; }

        public string PurchaseDate { get; set; }

        public string BuyDate { get; set; }

        public string VarSymb { get; set; }

        public string InvoiceCompanyName { get; set; }

        public string InvoiceFirstName { get; set; }

        public string InvoiceSurname { get; set; }

        public string InvoiceStreet { get; set; }

        public string InvoiceDescriptiveNumber { get; set; }

        public string InvoiceOrientationNumber { get; set; }

        public string InvoiceCity { get; set; }

        public string InvoiceZip { get; set; }

        public string InvoiceCountry { get; set; }

        public string InvoicePhone { get; set; }

        public string DeliveryCompanyName { get; set; }

        public string DeliveryName { get; set; }

        public string DeliverySurname { get; set; }

        public string DeliveryStreet { get; set; }

        public string DeliveryDescriptiveNumber { get; set; }

        public string DeliveryOrientationNumber { get; set; }

        public string DeliveryCity { get; set; }

        public string DeliveryZip { get; set; }

        public string DeliveryCountry { get; set; }

        public string DeliveryPhone { get; set; }

        public string CurrencyCode { get; set; }

        public string ErpStatusName { get; set; }

        public string ErpShippingName { get; set; }

        public string ErpPaymentName { get; set; }

        public string Customer { get; set; }

        public string Email { get; set; }

        public string UserId { get; set; }

        public string PayDate { get; set; }

        public string Paid { get; set; }

        public string ErpStatus { get; set; }

        public string NoteLeftByCustomer { get; set; }

        public string InternalNote { get; set; }

        public string InvoiceSent { get; set; }

        public string InvoiceId { get; set; }

        public string PreviewText { get; set; }

        public string DeliveryFormattedHouseNumber { get; set; }

        public string FormattedHouseNumber { get; set; }

        public bool IsPayOnDelivery { get; set; }

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
            throw new NotImplementedException();
        }

        [JsonProperty(nameof(IErpOrderModel.OrderPriceElements))]
        public List<ElerpPriceElement> PriceElements { get; set; } = new List<ElerpPriceElement>();

        [JsonProperty(nameof(IErpOrderModel.LineItems))]
        public List<ElerpOrderItemModel> Items { get; set; } = new List<ElerpOrderItemModel>();

        [JsonIgnore]
        public IEnumerable<IErpPriceElementModel> OrderPriceElements => PriceElements;

        [JsonIgnore]
        public IEnumerable<IErpOrderItemModel> LineItems => Items;

        public string DiscountsText { get; set; }

        [JsonIgnore]
        public DateTime Dt
        {
            get
            {
                return DateTime.Parse(PurchaseDate);
            }
        }

        public string CustomerErpUid => throw new NotImplementedException();
    }
}
