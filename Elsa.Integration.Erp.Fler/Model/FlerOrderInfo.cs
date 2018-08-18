// ReSharper disable InconsistentNaming

using System.Collections.Generic;

namespace Elsa.Integration.Erp.Fler.Model
{
    public class Order
    {
        public string id { get; set; }
        public string state { get; set; }
        public string date_created { get; set; }
        public string detail_link { get; set; }
        public string buyer_username { get; set; }
        public string buyer_avatar { get; set; }
        public string buyer_profile_link { get; set; }
        public string invoice_num { get; set; }
        public string invoice_link { get; set; }
        public string currency { get; set; }
        public int sum_items { get; set; }
        public int sum_items_withoutprovision { get; set; }
        public int sum_provision { get; set; }
        public int sum_postage { get; set; }
        public string conversion_rate_czk_to_eur { get; set; }
        public string conversion_date { get; set; }
        public int items_count { get; set; }
    }

    public class DeliveryMethod18Info
    {
        public int id { get; set; }
        public string title { get; set; }
        public string city { get; set; }
        public string address { get; set; }
        public string country { get; set; }
        public string url { get; set; }
    }

    public class Info
    {
        public string date_accepted { get; set; }
        public string date_marked_paid { get; set; }
        public string date_sent { get; set; }
        public int address_delivery_same_as_billing { get; set; }
        public int delivery_method_id { get; set; }
        public string delivery_method_label { get; set; }
        public DeliveryMethod18Info delivery_method_18_info { get; set; }
        public string delivery_date_sent { get; set; }
        public int payment_method_id { get; set; }
        public string payment_method_label { get; set; }
        public int payment_is_upfront { get; set; }
        public string payment_filter { get; set; }
        public string customer_mobile_number { get; set; }
        public string customer_mobile_number_with_country_prefix { get; set; }
        public string customer_mobile_country { get; set; }
    }

    public class AddressBilling
    {
        public string name { get; set; }
        public string company { get; set; }
        public string address { get; set; }
        public string city { get; set; }
        public string zip { get; set; }
        public string iso_country_code { get; set; }
        public string business_id { get; set; }
        public string vat_id { get; set; }
        public string country { get; set; }
    }

    public class AddressDelivery
    {
        public string name { get; set; }
        public string company { get; set; }
        public string address { get; set; }
        public string city { get; set; }
        public string zip { get; set; }
        public string iso_country_code { get; set; }
        public string business_id { get; set; }
        public string vat_id { get; set; }
        public string country { get; set; }
    }

    public class Item
    {
        public int id_item { get; set; }
        public int id_product { get; set; }
        public object intern_code { get; set; }
        public string product_name { get; set; }
        public string product_img_url_small { get; set; }
        public string product_url { get; set; }
        public string product_delivery { get; set; }
        public string currency { get; set; }
        public string price_without_provision { get; set; }
        public string price { get; set; }
        public int provision_pct { get; set; }
        public string confirm { get; set; }
    }

    public class RootObject
    {
        public Order order { get; set; }
        public Info info { get; set; }
        public AddressBilling address_billing { get; set; }
        public AddressDelivery address_delivery { get; set; }
        public List<Item> items { get; set; }
    }
}