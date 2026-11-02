using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;

using RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    internal class OrderMapperHost
    {
        private bool _stopMapping;

        private readonly IErpDataMapper _mapper;
        private readonly IErpOrderModel _erpOrder;

        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IDatabase _database;
        private readonly ICurrencyRepository _currencyRepository;

        private readonly IOrderStatusMappingRepository _statusMappingRepository;
        private readonly IProductRepository _productRepository;

        private IPurchaseOrder _order;

        public IPurchaseOrder Order
        {
            get
            {
                if (_order == null)
                {
                    throw new InvalidOperationException("orderObjectFactory must be called as first");
                }

                return _order;
            }

            private set
            {
                _order = value;
            }
        }

        public List<IOrderItem> Items { get; } = new List<IOrderItem>();

        public List<IOrderPriceElement> PriceElements { get; } = new List<IOrderPriceElement>();

        public IAddress InvoiceAddress { get; private set; }

        public IAddress DeliveryAddress { get; private set; }
        
        public List<long> OrderItemsToDelete { get; } = new List<long>();

        public List<long> PriceElementsToDelete { get; } = new List<long>();
        
        public ICurrency Currency { get; private set; } 

        public OrderMapperHost(IErpDataMapper mapper, IErpOrderModel erpOrder, IPurchaseOrderRepository purchaseOrderRepository, IDatabase database, ICurrencyRepository currencyRepository, IOrderStatusMappingRepository statusMappingRepository, IProductRepository productMappingRepository)
        {
            _mapper = mapper;
            _erpOrder = erpOrder;
            _purchaseOrderRepository = purchaseOrderRepository;
            _database = database;
            _currencyRepository = currencyRepository;
            _statusMappingRepository = statusMappingRepository;
            _productRepository = productMappingRepository;
        }

        public bool Map()
        {
            _mapper.MapOrder(
                _erpOrder,
                GetOrder,
                GetOrderItem,
                GetPriceElement,
                i => GetAddress(i.ErpOrderId, true),
                i => GetAddress(i.ErpOrderId, false),
                GetCurrency,
                _statusMappingRepository.GetMappings(_erpOrder.ErpSystemId),
                _productRepository);

            return !_stopMapping;
        }

        private IOrderPriceElement GetPriceElement(string arg)
        {
            var xid = long.Parse(arg);

            var priceElement = Order.PriceElements.FirstOrDefault(e => e.ExternalId == xid);
            if (priceElement != null) 
            {
                PriceElementsToDelete.Remove(priceElement.Id);
            }
            else 
            {
                priceElement = _database.New<IOrderPriceElement>(i => i.ExternalId = xid);
            }

            PriceElements.Add(priceElement);

            return priceElement;
        }

        private IPurchaseOrder GetOrder(OrderIdentifier identifier)
        {
            var order =  _purchaseOrderRepository.TryLoadOrderByOrderNumber(identifier.ErpOrderId);
            if (order == null)
            {
                order = _database.New<IPurchaseOrder>();
            }
            else if (order.OrderHash == identifier.OrderHash)
            {
                _stopMapping = true;
                Order = order;
                return null;
            }
            else
            {
                Order = order;
                OrderItemsToDelete.AddRange(Order.Items.Select(i => i.Id));
                PriceElementsToDelete.AddRange(Order.PriceElements.Select(i => i.Id));
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
                item = _database.New<IOrderItem>(i => i.ErpOrderItemId = erpItemId);
            }

            Items.Add(item);

            return item;
        }

        private IAddress GetAddress(string erpOrderId, bool isInvoiceAddress)
        {
            IAddress address = (isInvoiceAddress ? Order.InvoiceAddress : Order.DeliveryAddress)
                               ?? _database.New<IAddress>();

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
                Currency = _currencyRepository.GetCurrency(symbol) ?? _database.New<ICurrency>(c =>
                               {
                                   c.Symbol = symbol; 
                               });
            }

            return Currency;
        }
    }
}
