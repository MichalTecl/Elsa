using System;

using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Model;
using Elsa.Common.Logging;

namespace Elsa.Integration.Erp.Flox
{
    public class FloxDataMapper : ErpDataMapperBase
    {
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
            return 0;
        }

        protected override decimal ObtainPaymentTaxPercent(IErpOrderModel source)
        {
            return 0;
        }

        protected override decimal ObtainTaxedShippingCost(IErpOrderModel source)
        {
            return 0;
        }

        protected override decimal ObtainTaxedPaymentCost(IErpOrderModel source)
        {
            return 0;
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
    }
}

