using Newtonsoft.Json;

namespace Elsa.Integration.Erp.Fler.Model
{
    public class FlerOrderModel
    {
        [JsonProperty("id")]
        public int OrderId { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("buyer_billing_name")]
        public string BuyerName { get; set; }

        [JsonProperty("sum_items")]
        public decimal SumItems { get; set; }

        [JsonProperty("sum_postage")]
        public decimal SumPostage { get; set; }

        public decimal Price { get { return SumItems + SumPostage; } }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("payment_filter")]
        public string PaymentType { get; set; }
    }
}
