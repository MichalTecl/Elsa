using System;

using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Model;

namespace Elsa.Integration.Erp.Fler
{
    public class FlerDataMapper : ErpDataMapperBase
    {
        protected override bool HasDeliveryAddress(IErpOrderModel source)
        {
            return !string.IsNullOrEmpty(source.DeliveryCity);
        }

        protected override decimal ObtainShippingTaxPercent(IErpOrderModel source)
        {
            return -1m;
        }

        protected override decimal ObtainPaymentTaxPercent(IErpOrderModel source)
        {
            return -1m;
        }

        protected override decimal ObtainTaxedShippingCost(IErpOrderModel source)
        {
            return -1m;
        }

        protected override decimal ObtainTaxedPaymentCost(IErpOrderModel source)
        {
            return -1m;
        }

        protected override string MapShippingMethodName(IErpOrderModel source)
        {
            return source.ErpShippingName;
        }

        protected override string MapPaymentMethodName(IErpOrderModel source)
        {
            return source.ErpPaymentName;
        }

        protected override decimal ParseMoney(string source, IErpOrderModel sourceRecord, IErpOrderItemModel sourceItem, string sourcePropertyName)
        {
            decimal val;
            if (!decimal.TryParse(source, out val))
            {
                return 0;
            }

            return val;
        }

        protected override DateTime ParseDt(string source, IErpOrderModel sourceRecord, IErpOrderItemModel sourceItem, string sourcePropertyName)
        {
            return DateTime.Parse(source);
        }
    }
}
