using BwApiClient.Model;
using BwApiClient.Model.Data;
using BwApiClient.Model.Enums;
using BwApiClient.Model.Inputs;
using MTecl.GraphQlClient;
using MTecl.GraphQlClient.Execution;
using MTecl.GraphQlClient.ObjectMapping.GraphModel.Variables;
using MTecl.GraphQlClient.ObjectMapping.Rendering.JsonConvertors;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace BwApiClient
{
    public class BwClient
    {
        private readonly Func<HttpClient> _httpClientFactory;
        private readonly GqlRequestOptions _options;

        private static readonly GraphQlQueryBuilder<IBwApi> _builder = new GraphQlQueryBuilder<IBwApi>()
        {
            DateTimeConversionMode = DateTimeConverter.StringConversionMode("yyyy-MM-dd HH:mm:ss", alternativeFormats: new[] { "yyyy-MM-dd" })
        };

        private static readonly List<NotificationRequest> _defaultNotificationSettings = new List<NotificationRequest> { 
            new NotificationRequest 
            { 
                @if = new List<NotificationCondition> { NotificationCondition.and_SYSTEM_DEFAULT  }, 
                type = NotificationType.EMAIL_CUSTOMER
            } 
        };

        /// <summary>
        /// https://www.byznysweb.cz/a/1267/volani-api
        /// </summary>
        /// <param name="url">API naslouchá na adrese https://vase-stranka.flox.cz/api/graphql.</param>
        /// <param name="token">https://www.byznysweb.cz/a/1274/token-api</param>
        /// <param name="httpClientFactory"></param>
        public BwClient(string url, string token, Func<HttpClient> httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;

            _options = new GqlRequestOptions()
            {
                RequestUri = new Uri(url)
            };

            _options.CustomRequestHeaders["BW-API-Key"] = $"Token {token}";

            _options.CreateResponseException = (r, s) =>
            {
                return new Exception(s);
            };
        }

        public GqlRequestOptions Options => _options;

        private static readonly IQuery<List<OrderStatus>> _orderStatusesQuery = _builder.Build(q => q.ListOrderStatuses(null, QueryVariable.Pass<bool>("$onlyactive")));
        public async Task<List<OrderStatus>> GetDefinedOrderStatuses(bool onlyActive = true)
        {

            return await Execute(_orderStatusesQuery.WithVariable("$onlyactive", onlyActive));
        }

        private static readonly IQuery<PaginatedList<Order>> _ordersChangedAfterQuery = _builder.Build(
            q => q.GetOrderList(
                /*lang_code:*/ null,
                /*status:*/ QueryVariable.Pass<int>("$statusId", true),
                /*newer_from:*/ null, //QueryVariable.Pass<DateTime>("$newer_from", true, "DateTime"),
                /*changed_from:*/ QueryVariable.Pass<DateTime>("$changedAfter", true, "DateTime"),
                /*@params:*/ QueryVariable.Pass<OrderParams>("$params"),
                /*filter:*/ null).With(d => d.data.With(o => o.customer
                                                      //, o => o.creator
                                                      , o => o.vat_summary
                                                      , o => o.invoice_address
                                                      , o => o.delivery_address
                                                      //, o => o.shipments
                                                      , o => o.price_elements
                                                      , o => o.salesrep
                                                      , o => o.invoices
                                                      , o => o.preinvoices
                                                      , o => o.sum
                                                      , o => o.items.With(i => i.product)                                                      
                                                      )));
        public async Task<PaginatedList<Order>> GetOrders(DateTime? changedAfter = null, int? orderStatusId = null, int? listOffset = null, OrderSorting orderSorting = OrderSorting.last_change, Direction sortDirection = Direction.DESC)
        {
            var orderParams = new OrderParams
            {
                sort = sortDirection,
                order_by = orderSorting,
                cursor = listOffset,
            };


            return await Execute(_ordersChangedAfterQuery
                                    .WithVariable("$changedAfter", changedAfter ?? DateTime.MinValue)
                                    //.WithVariable("$newer_from", DateTime.Now.AddDays(-400))
                                    .WithVariable("$params", orderParams)
                                    .WithVariable("$statusId", orderStatusId));
        }

        private static readonly IQuery<Order> _orderQuery = _builder.Build(q => q.GetOrder(QueryVariable.Pass<string>("orderNr")).With(o => o.customer
                                                      //, o => o.creator
                                                      , o => o.vat_summary
                                                      , o => o.invoice_address
                                                      , o => o.delivery_address
                                                      //, o => o.shipments
                                                      , o => o.price_elements
                                                      , o => o.salesrep
                                                      , o => o.invoices
                                                      , o => o.preinvoices
                                                      , o => o.sum
                                                      , o => o.items.With(i => i.product)
                                                      ));
        public async Task<Order> GetOrder(string orderNumber)
        {
            return await Execute(_orderQuery.WithVariable("orderNr", orderNumber));
        }

        private static readonly IQuery<OrderLastChangeInfo> _orderLastChangeQuery = _builder.Build(q => q.GetOrderLastChangeInfo(QueryVariable.Pass<string>("orderNr")));
        public async Task<OrderLastChangeInfo> GetOrderLastChangeInfoAsync(string orderNr)
        {
            return await Execute(_orderLastChangeQuery.WithVariable("orderNr", orderNr));
        }

        private static readonly IQuery<OrderStatusInfo> _orderStatusChange = _builder.BuildMutation(q =>
        q.ChangeOrderStatus(
            QueryVariable.Pass<string>("$orderNr"),
            QueryVariable.Pass<int>("$status"),
            _defaultNotificationSettings
            ));
        public async Task<OrderStatusInfo> ChangeOrderStatus(string orderNumber, int status)
        {
            return await Execute(_orderStatusChange
                .WithVariable("$orderNr", orderNumber)
                .WithVariable("$status", status));
        }

        private static readonly IQuery<Invoice> _finalizeOrderInvoice = _builder.BuildMutation(m => m.FinalizeInvoice(QueryVariable.Pass<string>("preinvoiceNr"), _defaultNotificationSettings));
        public async Task<Invoice> FinalizeInvoice(string preinvoiceNumber)
        {
            return await Execute(_finalizeOrderInvoice.WithVariable("preinvoiceNr", preinvoiceNumber));
        }

        private static readonly IQuery<Order> _orderShallowQuery = _builder.Build(q => q.GetOrder(QueryVariable.Pass<string>("orderNr")));
        public async Task<Order> GetShallowOrder(string orderNumber)
        {
            return await Execute(_orderShallowQuery.WithVariable("orderNr", orderNumber));
        }

        private static readonly IQuery<PaginatedList<Product>> _productListQuery = _builder.Build(q => q.GetProductList("cz", QueryVariable.Pass<ProductParams>("$params"), null)
        .With(pl => pl.data.With(p => p.warehouse_items.With(whi => whi.price, whi => whi.attributes.With(atr => atr.values)))));

        public async Task<PaginatedList<Product>> GetProducts(int? cursor)
        {
            return await Execute(_productListQuery.WithVariable("$params", new ProductParams { cursor = cursor }));
        }


        private async Task<T> Execute<T>(IQuery<T> query)
        {
            using (var client = _httpClientFactory())
            {
                return await query.WithOptions(_options).ExecuteAsync<T>(client);
            }
        }
    }
}
