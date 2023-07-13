using System;
using System.Collections.Generic;

using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Integration;

namespace Elsa.Commerce.Core
{
    public interface IErpDataMapper
    {
        void MapOrder(
            IErpOrderModel source,
            Func<OrderIdentifier, IPurchaseOrder> orderObjectFactory,
            Func<string, IOrderItem> orderItemByErpOrderItemId,
            Func<string, IOrderPriceElement> priceElementFactory,
            Func<OrderIdentifier, IAddress> invoiceAddressFactory,
            Func<OrderIdentifier, IAddress> deliveryAddressFactory,
            Func<string, ICurrency> currencyByCurrencySymbol,
            IDictionary<string, IErpOrderStatusMapping> erpOrderStatusMappings,
            IProductRepository productRepository);
    }
}
