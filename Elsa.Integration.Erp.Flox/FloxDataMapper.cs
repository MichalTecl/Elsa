using System;
using System.Linq;

using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Model;
using Elsa.Common;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;

using Robowire.RobOrm.Core;

namespace Elsa.Integration.Erp.Flox
{
    public class FloxDataMapper : ErpDataMapperBase
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;

        public FloxDataMapper(IDatabase database, ISession session)
            : base(database, session)
        {
            m_database = database;
            m_session = session;
        }

        protected override int GetProductId(IErpOrderModel order, IErpOrderItemModel item)
        {
            var product =
                m_database.SelectFrom<IProduct>()
                    .Where(p => p.ProjectId == m_session.Project.Id && p.Name == item.ProductName)
                    .Execute()
                    .FirstOrDefault();

            if (product == null)
            {
                product = m_database.New<IProduct>();
                product.ProjectId = m_session.Project.Id;
                product.Name = item.ProductName;
                
                m_database.Save(product);
            }

            return product.Id;
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
            return source.ErpShippingName;
        }

        protected override string MapPaymentMethodName(IErpOrderModel source)
        {
            return source.ErpPaymentName;
        }

        protected override string ObtainElsaOrderStatus(IErpOrderModel source)
        {
            return source.ErpStatusName;
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

        protected override ICurrency GetElsaCurrency(string sourceCurrencySymbol)
        {
            var currency =
                m_database.SelectFrom<ICurrency>()
                    .Where(c => c.ProjectId == m_session.Project.Id && c.Symbol == sourceCurrencySymbol)
                    .Execute()
                    .FirstOrDefault();

            if (currency == null)
            {
                currency = m_database.New<ICurrency>();
                currency.Symbol = sourceCurrencySymbol;

                m_database.Save(currency);
            }

            return currency;
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

