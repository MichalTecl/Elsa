using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEmailingApi.Client.Messages
{
    using System.Text.Json.Serialization;

    public class Price
    {
        [JsonPropertyName("without_vat")]
        public decimal WithoutVat { get; set; }
     
        [JsonPropertyName("with_vat")]
        public decimal WithVat { get; set; }
        
        [JsonPropertyName("currency")]
        public string Currency { get; set; }
    }

    public class Attribute
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

    public class Item
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("price")]
        public Price Price { get; set; }
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; }
        [JsonPropertyName("attributes")]
        public Attribute[] Attributes { get; set; }
    }

    public class Data
    {
        [JsonPropertyName("eshop_name")]
        public string EshopName { get; set; }
        
        [JsonPropertyName("eshop_code")]
        public string EshopCode { get; set; }
        
        [JsonPropertyName("emailaddress")]
        public string EmailAddress { get; set; }
        
        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }
        
        [JsonPropertyName("attributes")]
        public Attribute[] Attributes { get; set; }
        
        [JsonPropertyName("items")]
        public Item[] Items { get; set; }
    }

    public class ImportOrdersRequest
    {
        [JsonPropertyName("settings")]
        public Settings Settings { get; set; }

        [JsonPropertyName("data")]
        public Data[] Data { get; set; }
    }

    public class Settings
    {
        [JsonPropertyName("skip_invalid_orders")]
        public bool SkipInvalidOrders { get; set; }
    }

}
