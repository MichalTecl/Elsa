﻿using System;
using System.Collections.Generic;

using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Integration;

namespace Elsa.Commerce.Core
{
    public abstract class ErpDataMapperBase : IErpDataMapper
    {

        public void MapOrder(
            IErpOrderModel source,
            Func<OrderIdentifier, IPurchaseOrder> orderObjectFactory,
            Func<string, IOrderItem> orderItemByErpOrderItemId,
            Func<string, IOrderPriceElement> priceElementFactory,
            Func<OrderIdentifier, IAddress> invoiceAddressFactory,
            Func<OrderIdentifier, IAddress> deliveryAddressFactory,
            Func<string, ICurrency> currencyByCurrencySymbol,
            IDictionary<string, IErpOrderStatusMapping> erpOrderStatusMappings,
            IProductRepository productRepository)
        {
            var ordid = new OrderIdentifier(GetUniqueOrderNumber(source), source.OrderHash);

            var target = orderObjectFactory(ordid);
            if (target == null)
            {
                return;
            }
            
            MapTopLevelProperties(source, target);

            foreach (var sourceItem in source.LineItems)
            {
                var erpItemId = GetUniqueErpItemId(source, sourceItem);
                var targetItem = orderItemByErpOrderItemId(erpItemId);

                targetItem.ErpOrderItemId = erpItemId;
                targetItem.ErpProductId = sourceItem.ErpProductId;

                if (!string.IsNullOrWhiteSpace(sourceItem.ErpWarehouseItemCode)) 
                {
                    targetItem.ErpWarehouseItemCode = sourceItem.ErpWarehouseItemCode;
                }

                targetItem.ErpWarehouseItemId = string.IsNullOrWhiteSpace(sourceItem.ErpWarehouseItemId) ? null : sourceItem.ErpWarehouseItemId;
                targetItem.PlacedName = sourceItem.ProductName;
                targetItem.Quantity = sourceItem.Quantity;
                targetItem.TaxPercent = sourceItem.TaxPercent;
                targetItem.TaxedPrice = ParseMoney(sourceItem.TaxedPrice, source, sourceItem, nameof(sourceItem.TaxedPrice));

                var parsedItemWeight = TryParseWeight(sourceItem.ProductItemWeight, source, sourceItem) ;

                if (parsedItemWeight != null)
                {
                    targetItem.Weight = targetItem.Quantity * parsedItemWeight;
                }
                else
                {
                    targetItem.Weight = null;
                }

                var product = productRepository.GetProduct(
                    source.ErpSystemId,
                    sourceItem.ErpProductId,
                    target.PurchaseDate,
                    targetItem.PlacedName);

                if (product == null)
                {
                    throw new InvalidOperationException("Cannot process product");
                }

                targetItem.ProductId = product.Id;
            }

            foreach(var sourcePriceElement in source.OrderPriceElements) 
            {
                var erpElementId = GetUniqueErpPriceElementId(source, sourcePriceElement);

                var targetPriceElement = priceElementFactory(erpElementId);

                targetPriceElement.ExternalId = long.Parse(sourcePriceElement.ErpPriceElementId);
                targetPriceElement.TypeName = sourcePriceElement.TypeErpName;
                targetPriceElement.Title = sourcePriceElement.Title;
                targetPriceElement.Tax = ParseMoneyOrNull(sourcePriceElement.TaxPercent, null, null, nameof(sourcePriceElement.TaxPercent));
                targetPriceElement.Price = ParseMoneyOrNull(sourcePriceElement.Price, null, null, nameof(sourcePriceElement.Price));
            }

            var invoiceAddress = invoiceAddressFactory(ordid);
            MapInvoiceAddress(source, invoiceAddress);

            if (HasDeliveryAddress(source))
            {
                var deliveryAddress = deliveryAddressFactory(ordid);
                MapDeliveryAddress(source, deliveryAddress);
            }

            currencyByCurrencySymbol(source.CurrencyCode);

            target.OrderStatusId = ResolveOrderStatusId(source, erpOrderStatusMappings);

            if (OrderStatus.IsReturned(target.OrderStatusId) && (target.ReturnDt == null))
            {
                target.ReturnDt = DateTime.Now.Date;
            }
        }

        protected virtual int ResolveOrderStatusId(IErpOrderModel source, IDictionary<string, IErpOrderStatusMapping> erpOrderStatusMappings)
        {
            IErpOrderStatusMapping status;
            if (!erpOrderStatusMappings.TryGetValue(source.ErpStatus, out status))
            {
                throw new InvalidOperationException($"Status mapping not found for ErpId={source.ErpSystemId} and Status=\"{source.ErpStatus}\" OrderNumber=\"{source.OrderNumber}\"");
            }

            return status.OrderStatusId;
        }

        protected virtual void MapTopLevelProperties(IErpOrderModel source, IPurchaseOrder target)
        {
            target.ErpOrderId = source.ErpOrderId;
            target.OrderNumber = source.OrderNumber;
            target.OrderHash = source.OrderHash;
            target.PreInvoiceId = source.PreInvId;
            target.InvoiceId = source.InvoiceId;
            target.CustomerName = source.Customer;
            target.CustomerEmail = source.Email;
            target.CustomerNote = source.NoteLeftByCustomer;
            target.InternalNote = source.InternalNote;
            target.VarSymbol = source.VarSymb ?? string.Empty;
            target.ErpStatusId = source.ErpStatus ?? string.Empty;
            target.ErpStatusName = source.ErpStatusName ?? string.Empty;
            
            target.Price = ParseMoney(source.Price, source, null, nameof(source.Price));
            target.PriceWithVat = ParseMoney(source.PriceWithVat, source, null, nameof(source.PriceWithVat));
            
            target.ShippingMethodName = MapShippingMethodName(source);
            target.PaymentMethodName = MapPaymentMethodName(source);
            target.TaxedShippingCost = ObtainTaxedShippingCost(source);
            target.TaxedPaymentCost = ObtainTaxedPaymentCost(source);
            target.ShippingTaxPercent = ObtainShippingTaxPercent(source);
            target.PaymentTaxPercent = ObtainPaymentTaxPercent(source);
            target.PercentDiscountText = ObtainPercentDiscountText(source);
            target.PercentDiscountValue = ObtainPercentDiscountValue(source);

            target.PurchaseDate = ParseDt(source.PurchaseDate, source, null, nameof(source.PurchaseDate));
            target.BuyDate = ParseDt(source.BuyDate, source, null, nameof(source.BuyDate));
            target.DueDate = ParseDt(source.BuyDate, source, null, nameof(source.BuyDate));
            target.IsPayOnDelivery = source.IsPayOnDelivery;
            target.DiscountsText = source.DiscountsText;
            target.CustomerErpUid = source.CustomerErpUid;

            target.VatId = source.VatId;
            target.CompanyRegId = source.CompanyRegistrationId;
            target.ErpLastChange = ObtainErpLastchangeDate(source);
        }

        protected abstract decimal? ObtainPercentDiscountValue(IErpOrderModel source);
        protected abstract string ObtainPercentDiscountText(IErpOrderModel source);
        protected abstract DateTime ObtainErpLastchangeDate(IErpOrderModel source);


        protected abstract bool HasDeliveryAddress(IErpOrderModel source);

        protected virtual void MapInvoiceAddress(IErpOrderModel source, IAddress target)
        {
            target.Country = source.InvoiceCountry ?? string.Empty;
            target.DescriptiveNumber = source.InvoiceDescriptiveNumber ?? string.Empty;
            target.FirstName = source.InvoiceFirstName ?? string.Empty;
            target.LastName = source.InvoiceSurname ?? string.Empty;
            target.OrientationNumber = source.InvoiceOrientationNumber ?? string.Empty;
            target.Phone = source.InvoicePhone ?? string.Empty;
            target.Street = source.InvoiceStreet ?? string.Empty;
            target.Zip = Limit(source.InvoiceZip ?? string.Empty, 16);
            target.City = source.InvoiceCity ?? string.Empty;
            target.CompanyName = source.InvoiceCompanyName ?? string.Empty;
        }

        protected virtual void MapDeliveryAddress(IErpOrderModel source, IAddress target)
        {
            target.Country = source.DeliveryCountry ?? string.Empty;
            target.DescriptiveNumber = source.DeliveryDescriptiveNumber ?? string.Empty;
            target.FirstName = source.DeliveryName ?? string.Empty;
            target.LastName = source.DeliverySurname ?? string.Empty;
            target.OrientationNumber = source.DeliveryOrientationNumber ?? string.Empty;
            target.Phone = source.DeliveryPhone ?? string.Empty;
            target.Street = source.DeliveryStreet ?? string.Empty;
            target.Zip = source.DeliveryZip ?? string.Empty;
            target.City = source.DeliveryCity ?? string.Empty;
            target.CompanyName = source.DeliveryCompanyName ?? string.Empty;
        }

        protected abstract decimal ObtainShippingTaxPercent(IErpOrderModel source);

        protected abstract decimal ObtainPaymentTaxPercent(IErpOrderModel source);

        protected abstract decimal ObtainTaxedShippingCost(IErpOrderModel source);

        protected abstract decimal ObtainTaxedPaymentCost(IErpOrderModel source);

        protected abstract string MapShippingMethodName(IErpOrderModel source);

        protected abstract string MapPaymentMethodName(IErpOrderModel source);
        
        protected abstract decimal ParseMoney(string source, IErpOrderModel sourceRecord, IErpOrderItemModel sourceItem, string sourcePropertyName);

        protected abstract decimal? ParseMoneyOrNull(string source, IErpOrderModel sourceRecord, IErpOrderItemModel sourceItem, string sourcePropertyName);

        protected abstract decimal? TryParseWeight(string source, IErpOrderModel sourceRecord,
            IErpOrderItemModel sourceItem);

        protected abstract DateTime ParseDt(string source, IErpOrderModel sourceRecord, IErpOrderItemModel sourceItem, string sourcePropertyName);

        protected virtual string GetUniqueOrderNumber(IErpOrderModel order)
        {
            return order.OrderNumber;
        }

        protected virtual string GetUniqueErpItemId(IErpOrderModel order, IErpOrderItemModel item)
        {
            return item.ErpOrderItemId;
        }

        protected virtual string GetUniqueErpPriceElementId(IErpOrderModel order, IErpPriceElementModel element) 
        {
            return element.ErpPriceElementId;
        }

        protected static string Limit(string s, int len)
        {
            if (s.Length > len)
            {
                return s.Substring(0, len);
            }

            return s;
        }

        public virtual DateTime GetPurchaseDate(IErpOrderModel source)
        {
            return ParseDt(source.PurchaseDate, source, null, nameof(source.PurchaseDate));
        }
    }
}
