using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Model;
using Elsa.Common;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core
{
    public abstract class ErpDataMapperBase : IErpDataMapper
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;

        protected ErpDataMapperBase(IDatabase database, ISession session)
        {
            m_database = database;
            m_session = session;
        }

        public IPurchaseOrder MapOrder(IErpOrderModel source, Action<IPurchaseOrder> onBeforeSave)
        {
            var orderNumber = GetUniqueOrderNumber(source);

            var erpId = source.ErpSystemId;
            var projectId = m_session.Project.Id;

            var target =
                m_database.SelectFrom<IPurchaseOrder>()
                    .Join(p => p.Items)
                    .Join(p => p.DeliveryAddress)
                    .Join(p => p.InvoiceAddress)
                    .Where(p => p.ErpId == erpId)
                    .Where(p => p.ProjectId == projectId)
                    .Where(p => p.OrderNumber == orderNumber)
                    .Execute()
                    .FirstOrDefault();

            target = target ?? m_database.New<IPurchaseOrder>();

            onBeforeSave(target);

            if (target.Id == 0)
            {
                target.ProjectId = projectId;
                target.ErpId = erpId;
                target.OrderNumber = orderNumber;
                target.InsertUserId = m_session.User.Id;
                target.InsertDt = DateTime.Now;
            }

            MapTopLevelProperties(source, target);

            m_database.Save(target);

            var updatedItems = new HashSet<string>();
            foreach (var sourceItem in source.LineItems)
            {
                var erpItemNumber = GetUniqueErpItemId(source, sourceItem);
                updatedItems.Add(erpItemNumber);

                var targetItem = target.Items?.FirstOrDefault(i => i.ErpOrderItemId == erpItemNumber) ?? m_database.New<IOrderItem>();

                targetItem.PurchaseOrderId = target.Id;
                targetItem.ErpOrderItemId = erpItemNumber;
                targetItem.PlacedName = sourceItem.ProductName;
                targetItem.ProductId = GetProductId(source, sourceItem);
                targetItem.Quantity = sourceItem.Quantity;
                targetItem.TaxPercent = ParseMoney(sourceItem.TaxPercent, source, sourceItem, nameof(sourceItem.TaxPercent));
                targetItem.TaxedPrice = ParseMoney(
                    sourceItem.TaxedPrice,
                    source,
                    sourceItem,
                    nameof(targetItem.TaxedPrice));

                m_database.Save(targetItem);
            }


            var itemsToDelete = target.Items?.Where(srcItem => !updatedItems.Contains(srcItem.ErpOrderItemId));
            if (itemsToDelete != null)
            {
                foreach (var i in itemsToDelete)
                {
                    m_database.Delete(i);
                }
            }

            return target;
        }

        protected abstract int GetProductId(IErpOrderModel order, IErpOrderItemModel item);

        protected virtual void MapTopLevelProperties(IErpOrderModel source, IPurchaseOrder target)
        {
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
            target.PriceWithVat = ParseMoney(source.Price, source, null, nameof(source.Price));

            target.ElsaOrderStatus = ObtainElsaOrderStatus(source);
            target.ShippingMethodName = MapShippingMethodName(source);
            target.PaymentMethodName = MapPaymentMethodName(source);
            target.TaxedShippingCost = ObtainTaxedShippingCost(source);
            target.TaxedPaymentCost = ObtainTaxedPaymentCost(source);
            target.ShippingTaxPercent = ObtainShippingTaxPercent(source);
            target.PaymentTaxPercent = ObtainPaymentTaxPercent(source);

            target.PurchaseDate = ParseDt(source.PurchaseDate, source, null, nameof(source.PurchaseDate));
            target.BuyDate = ParseDt(source.BuyDate, source, null, nameof(source.BuyDate));
            target.DueDate = ParseDt(source.BuyDate, source, null, nameof(source.BuyDate));

            target.CurrencyId = GetElsaCurrency(source.CurrencyCode).Id;
            
            var invoiceAddress = target.InvoiceAddress ?? m_database.New<IAddress>();
            MapInvoiceAddress(source, invoiceAddress);
            m_database.Save(invoiceAddress);
            target.InvoiceAddressId = invoiceAddress.Id;

            if (HasDeliveryAddress(source))
            {
                var deliveryAddress = target.DeliveryAddress ?? m_database.New<IAddress>();
                MapDeliveryAddress(source, deliveryAddress);
                m_database.Save(deliveryAddress);
                target.DeliveryAddressId = deliveryAddress.Id;
            }


        }

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

        protected abstract string ObtainElsaOrderStatus(IErpOrderModel source);

        protected abstract decimal ParseMoney(string source, IErpOrderModel sourceRecord, IErpOrderItemModel sourceItem, string sourcePropertyName);

        protected abstract DateTime ParseDt(string source, IErpOrderModel sourceRecord, IErpOrderItemModel sourceItem, string sourcePropertyName);

        protected virtual string GetUniqueOrderNumber(IErpOrderModel order)
        {
            return order.ErpOrderId;
        }

        protected virtual string GetUniqueErpItemId(IErpOrderModel order, IErpOrderItemModel item)
        {
            return item.ErpOrderItemId;
        }

        protected abstract ICurrency GetElsaCurrency(string sourceCurrencySymbol);

        protected static string Limit(string s, int len)
        {
            if (s.Length > len)
            {
                return s.Substring(0, 16);
            }

            return s;
        }
    }
}
