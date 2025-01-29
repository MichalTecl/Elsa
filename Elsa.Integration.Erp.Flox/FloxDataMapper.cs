using System;
using System.Linq;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Model;
using Elsa.Common.Logging;

namespace Elsa.Integration.Erp.Flox
{
    public class FloxDataMapper : ErpDataMapperBase
    {
        private const string SHIPPING_PRICE_ELEMENT_TYPE = "shipping";
        private const string PAYMENT_PRICE_ELEMENT_TYPE = "payment";
        private const string PERCENT_DISCOUNT_PRCELEMENT = "percent_discount";

        private readonly ILog m_log;

        public FloxDataMapper(ILog log)
        {
            m_log = log;
        }

        protected override bool HasDeliveryAddress(IErpOrderModel source)
        {
            return !string.IsNullOrWhiteSpace(source.DeliveryName);
        }

        protected override decimal ObtainShippingTaxPercent(IErpOrderModel source)
        {
            var s = source.OrderPriceElements.FirstOrDefault(p => p.TypeErpName.Equals(SHIPPING_PRICE_ELEMENT_TYPE, StringComparison.InvariantCultureIgnoreCase));

            return ParseMoneyOrNull(s.TaxPercent, source, null, nameof(s.TaxPercent)) ?? 0m;
        }

        protected override decimal ObtainPaymentTaxPercent(IErpOrderModel source)
        {
            var s = source.OrderPriceElements.FirstOrDefault(p => p.TypeErpName.Equals(PAYMENT_PRICE_ELEMENT_TYPE, StringComparison.InvariantCultureIgnoreCase));

            return ParseMoneyOrNull(s.TaxPercent, source, null, nameof(s.TaxPercent)) ?? 0m;
        }

        protected override decimal ObtainTaxedShippingCost(IErpOrderModel source)
        {
            var taxMultiplier = (ObtainShippingTaxPercent(source) / 100m) + 1m;
            var s = source.OrderPriceElements.FirstOrDefault(p => p.TypeErpName.Equals(SHIPPING_PRICE_ELEMENT_TYPE, StringComparison.InvariantCultureIgnoreCase));
            return (ParseMoneyOrNull(s.Price, source, null, nameof(s.Price)) ?? 0) * taxMultiplier;
        }

        protected override decimal ObtainTaxedPaymentCost(IErpOrderModel source)
        {
            var taxMultiplier = (ObtainPaymentTaxPercent(source) / 100m) + 1m;
            var s = source.OrderPriceElements.FirstOrDefault(p => p.TypeErpName.Equals(PAYMENT_PRICE_ELEMENT_TYPE, StringComparison.InvariantCultureIgnoreCase));
            return (ParseMoneyOrNull(s.Price, source, null, nameof(s.Price)) ?? 0) * taxMultiplier;
        }

        protected override string MapShippingMethodName(IErpOrderModel source)
        {
            return source.ErpShippingName?.Trim();
        }

        protected override string MapPaymentMethodName(IErpOrderModel source)
        {
            return source.ErpPaymentName?.Trim();
        }
        
        protected override decimal ParseMoney(string source, IErpOrderModel sourceRecord, IErpOrderItemModel sourceItem, string sourcePropertyName)
        {
            decimal d;
            if (decimal.TryParse(source, out d))
            {
                return d;
            }
            return 0;
        }

        protected override decimal? ParseMoneyOrNull(string source, IErpOrderModel sourceRecord, IErpOrderItemModel sourceItem, string sourcePropertyName)
        {
            if (string.IsNullOrEmpty(source))
                return null;

            return ParseMoney(source, sourceRecord, sourceItem, sourcePropertyName);
        }

        protected override decimal? TryParseWeight(string source, IErpOrderModel sourceRecord, IErpOrderItemModel sourceItem)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return null;
            }

            if (decimal.TryParse(source, out var parsed))
            {
                return parsed;
            }

            m_log.Error($"Cannot parse input string \"{source}\" as decimal. OrderNo={sourceRecord.OrderNumber}");
            return null;
        }

        protected override DateTime ParseDt(string source, IErpOrderModel sourceRecord, IErpOrderItemModel sourceItem, string sourcePropertyName)
        {
            return DateTime.Parse(source);
        }
        
        protected override string GetUniqueErpItemId(IErpOrderModel order, IErpOrderItemModel item)
        {
            var nr = base.GetUniqueErpItemId(order, item);

            if (string.IsNullOrWhiteSpace(nr))
            {
                nr = $"{order.OrderNumber}.{item.ProductName}";
            }

            return nr;
        }

        protected override decimal? ObtainPercentDiscountValue(IErpOrderModel source)
        {
            var e = source.OrderPriceElements.FirstOrDefault(i => i.TypeErpName.Equals(PERCENT_DISCOUNT_PRCELEMENT, StringComparison.InvariantCultureIgnoreCase));
            if (e == null)
                return null;

            if(!decimal.TryParse(e.Value, out var value))
            {
                m_log.Error($"Objednavka cislo {source.OrderNumber} ma element {PERCENT_DISCOUNT_PRCELEMENT} s neocekavanou hodnotou \"{e.Value}\"");
                return null;
            }

            return value;
        }

        protected override string ObtainPercentDiscountText(IErpOrderModel source)
        {
            var elms = source.OrderPriceElements.Where(e => e.TypeErpName.Equals(PERCENT_DISCOUNT_PRCELEMENT, StringComparison.InvariantCultureIgnoreCase)).ToArray();
            if (elms.Length > 1)
            {
                m_log.Error($"Objednavka cislo {source.OrderNumber} ma vice nez jeden element {PERCENT_DISCOUNT_PRCELEMENT} - nezpracuje se");
                return "CHYBA";
            }

            return elms.FirstOrDefault()?.Title;
        }

        protected override DateTime ObtainErpLastchangeDate(IErpOrderModel source)
        {
            return DateTime.Parse(source.ErpLastChangeDt);
        }
    }
}

