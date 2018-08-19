using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    internal class OrderMapperHost
    {
        private bool m_stopMapping;

        private readonly IErpDataMapper m_mapper;
        private readonly IErpOrderModel m_erpOrder;

        private readonly IPurchaseOrderRepository m_purchaseOrderRepository;
        private readonly IDatabase m_database;
        private readonly ICurrencyRepository m_currencyRepository;

        private readonly IOrderStatusMappingRepository m_statusMappingRepository;
        private readonly IProductMappingRepository m_productMappingRepository;

        private IPurchaseOrder m_order;

        public IPurchaseOrder Order
        {
            get
            {
                if (m_order == null)
                {
                    throw new InvalidOperationException("orderObjectFactory must be called as first");
                }

                return m_order;
            }

            private set
            {
                m_order = value;
            }
        }

        public List<IOrderItem> Items { get; } = new List<IOrderItem>();

        public IAddress InvoiceAddress { get; private set; }

        public IAddress DeliveryAddress { get; private set; }
        
        public List<long> OrderItemsToDelete { get; } = new List<long>();
        
        public ICurrency Currency { get; private set; } 

        public OrderMapperHost(IErpDataMapper mapper, IErpOrderModel erpOrder, IPurchaseOrderRepository purchaseOrderRepository, IDatabase database, ICurrencyRepository currencyRepository, IOrderStatusMappingRepository statusMappingRepository, IProductMappingRepository productMappingRepository)
        {
            m_mapper = mapper;
            m_erpOrder = erpOrder;
            m_purchaseOrderRepository = purchaseOrderRepository;
            m_database = database;
            m_currencyRepository = currencyRepository;
            m_statusMappingRepository = statusMappingRepository;
            m_productMappingRepository = productMappingRepository;
        }

        public bool Map()
        {
            m_mapper.MapOrder(
                m_erpOrder,
                GetOrder,
                GetOrderItem,
                i => GetAddress(i.ErpOrderId, true),
                i => GetAddress(i.ErpOrderId, false),
                GetCurrency,
                m_statusMappingRepository.GetMappings(m_erpOrder.ErpSystemId),
                m_productMappingRepository.GetMappings(m_erpOrder.ErpSystemId));

            return !m_stopMapping;
        }

        private IPurchaseOrder GetOrder(OrderIdentifier identifier)
        {
            var order =  m_purchaseOrderRepository.TryLoadOrderByOrderNumber(identifier.ErpOrderId);
            if (order == null)
            {
                order = m_database.New<IPurchaseOrder>();
            }
            else if (order.OrderHash == identifier.OrderHash)
            {
                m_stopMapping = true;
                Order = order;
                return null;
            }
            else
            {
                Order = order;
                OrderItemsToDelete.AddRange(Order.Items.Select(i => i.Id));
            }

            order.OrderNumber = identifier.ErpOrderId;
            Order = order;

            return Order;
        }

        private IOrderItem GetOrderItem(string erpItemId)
        {
            var item = Order.Items.FirstOrDefault(i => i.ErpOrderItemId == erpItemId);
            if (item != null)
            {
                OrderItemsToDelete.Remove(item.Id);
            }
            else
            {
                item = m_database.New<IOrderItem>(i => i.ErpOrderItemId = erpItemId);
            }

            Items.Add(item);

            return item;
        }

        private IAddress GetAddress(string erpOrderId, bool isInvoiceAddress)
        {
            IAddress address = (isInvoiceAddress ? Order.InvoiceAddress : Order.DeliveryAddress)
                               ?? m_database.New<IAddress>();

            if (isInvoiceAddress)
            {
                InvoiceAddress = address;
            }
            else
            {
                DeliveryAddress = address;
            }

            return address;
        }
        
        private ICurrency GetCurrency(string symbol)
        {
            if (Order.Currency?.Symbol == symbol)
            {
                Currency = Order.Currency;
            }
            else
            {
                Currency = m_currencyRepository.GetCurrency(symbol) ?? m_database.New<ICurrency>(c =>
                               {
                                   c.Symbol = symbol; 
                               });
            }

            return Currency;
        }
    }
}
