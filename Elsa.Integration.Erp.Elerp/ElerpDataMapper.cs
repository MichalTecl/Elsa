using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Integration;

namespace Elsa.Integration.Erp.Elerp
{
    public class ElerpDataMapper : ErpDataMapperBase
    {
        protected override bool HasDeliveryAddress(IErpOrderModel source)
        {
            return !string.IsNullOrWhiteSpace(source.DeliveryName);
        }

        protected override decimal ObtainShippingTaxPercent(IErpOrderModel source)
        {
            return 0m;
        }

        protected override decimal ObtainPaymentTaxPercent(IErpOrderModel source)
        {
            return 0m;
        }

        protected override decimal ObtainTaxedShippingCost(IErpOrderModel source)
        {
            return 0m;
        }

        protected override decimal ObtainTaxedPaymentCost(IErpOrderModel source)
        {
            return 0m;
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
            return decimal.Parse(source);
        }

        protected override decimal? TryParseWeight(string source, IErpOrderModel sourceRecord, IErpOrderItemModel sourceItem)
        {
            return null;
        }

        protected override DateTime ParseDt(string source, IErpOrderModel sourceRecord, IErpOrderItemModel sourceItem, string sourcePropertyName)
        {
            return DateTime.Parse(source);
        }

        protected override int ResolveOrderStatusId(IErpOrderModel source, IDictionary<string, IErpOrderStatusMapping> erpOrderStatusMappings)
        {
            return int.Parse(source.ErpStatus);
        }
    }
}
