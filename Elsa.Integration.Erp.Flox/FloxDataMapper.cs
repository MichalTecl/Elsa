using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Model;
using Elsa.Common;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Integration;

using Robowire.RobOrm.Core;

namespace Elsa.Integration.Erp.Flox
{
    public class FloxDataMapper : ErpDataMapperBase
    {
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
            return source.ErpShippingName;
        }

        protected override string MapPaymentMethodName(IErpOrderModel source)
        {
            return source.ErpPaymentName;
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

