using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Common.Interfaces;
using Robowire.RobOrm.Core;

namespace Elsa.Jobs.BulletinGeneration
{
    public sealed class BulletinDataRepository
    {
        private readonly IDatabase _database;
        private readonly ISession _session;
        private DateTime? _generatedAt;
        private List<BulletinOrder> _orders;
        private List<BulletinItem> _items;
        private Dictionary<string, DateTime> _firstPurchases;
        private List<BulletinSalesRep> _salesReps;

        public BulletinDataRepository(IDatabase database, ISession session)
        {
            _database = database;
            _session = session;
        }

        private void Begin(DateTime generatedAt)
        {
            if (_generatedAt == generatedAt) return;
            _generatedAt = generatedAt;
            _orders = null;
            _items = null;
            _firstPurchases = null;
            _salesReps = null;
        }

        public IReadOnlyList<BulletinOrder> Orders(DateTime generatedAt)
        {
            Begin(generatedAt);
            if (_orders != null) return _orders;
            var start = new DateTime(generatedAt.Year, generatedAt.Month, 1).AddMonths(-24);
            var orders = _database.Sql().ExecuteWithParams(@"SELECT po.Id, po.PurchaseDate,
                po.CustomerErpUid, po.CustomerName, " + BulletinQueries.CustomerKey + @" AS CustomerKey,
                ISNULL(c.IsDistributor, 0) AS IsDistributor, revenue.NetRevenue,
                CASE WHEN firstItem.TaxPercent IS NULL OR firstItem.TaxPercent < 0 THEN 1 ELSE 0 END AS InvalidTax
                " + BulletinQueries.OrderFrom + @"
                WHERE po.ProjectId = {0} AND po.OrderStatusId = 5
                  AND po.PurchaseDate >= {1} AND po.PurchaseDate < {2}", _session.Project.Id, start, generatedAt)
                .AutoMap<BulletinOrder>();
            if (orders.Any(o => o.InvalidTax != 0 || o.NetRevenue == null))
                throw new InvalidOperationException("Nelze určit tržby bez DPH u některých objednávek Bulletinu.");
            _orders = orders;
            return _orders;
        }

        public IReadOnlyList<BulletinItem> Items(DateTime generatedAt)
        {
            Begin(generatedAt);
            if (_items != null) return _items;
            var period = new BulletinPeriod(generatedAt);
            // Kit children are fulfillment details, not additional sold products.
            _items = _database.Sql().ExecuteWithParams(@"SELECT oi.Id, oi.PurchaseOrderId,
                oi.ProductId, po.ErpId, oi.ErpProductId, COALESCE(p.Name, oi.PlacedName, N'Neznámý výrobek') AS Name,
                oi.Quantity, oi.TaxedPrice
                FROM OrderItem oi JOIN PurchaseOrder po ON po.Id = oi.PurchaseOrderId
                LEFT JOIN Product p ON p.Id = oi.ProductId AND p.ProjectId = po.ProjectId
                WHERE po.ProjectId = {0} AND po.OrderStatusId = 5 AND oi.KitParentId IS NULL
                  AND ((po.PurchaseDate >= {1} AND po.PurchaseDate < {2})
                    OR (po.PurchaseDate >= {3} AND po.PurchaseDate < {4}))",
                _session.Project.Id, period.Start, period.End, period.PreviousStart, period.PreviousEnd)
                .AutoMap<BulletinItem>();
            return _items;
        }

        public IReadOnlyDictionary<string, DateTime> FirstPurchases(DateTime generatedAt)
        {
            Begin(generatedAt);
            if (_firstPurchases != null) return _firstPurchases;
            // All available history is needed to distinguish genuinely new customers.
            var rows = _database.Sql().ExecuteWithParams(@"SELECT " + BulletinQueries.CustomerKey + @" AS CustomerKey,
                MIN(po.PurchaseDate) AS FirstPurchase
                FROM PurchaseOrder po
                WHERE po.ProjectId = {0} AND po.OrderStatusId = 5 AND po.PurchaseDate < {1}
                GROUP BY " + BulletinQueries.CustomerKey, _session.Project.Id, generatedAt).AutoMap<BulletinFirstPurchase>();
            _firstPurchases = rows.Where(r => !string.IsNullOrEmpty(r.CustomerKey))
                .GroupBy(r => r.CustomerKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Min(r => r.FirstPurchase), StringComparer.OrdinalIgnoreCase);
            return _firstPurchases;
        }

        public IReadOnlyList<BulletinSalesRep> SalesReps(DateTime generatedAt)
        {
            Begin(generatedAt);
            if (_salesReps != null) return _salesReps;
            _salesReps = _database.Sql().ExecuteWithParams(@"SELECT DISTINCT c.ErpUid,
                COALESCE(sr.PublicName, sr.NameInErp, N'Neznámý zástupce') AS Name
                FROM Customer c JOIN SalesRepCustomer src ON src.CustomerId = c.Id
                JOIN SalesRepresentative sr ON sr.Id = src.SalesRepId AND sr.ProjectId = c.ProjectId
                WHERE c.ProjectId = {0} AND src.ValidFrom <= {1}
                  AND (src.ValidTo IS NULL OR src.ValidTo >= {1})", _session.Project.Id, generatedAt)
                .AutoMap<BulletinSalesRep>();
            return _salesReps;
        }
    }

    public sealed class BulletinOrder
    {
        public long Id { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string CustomerErpUid { get; set; }
        public string CustomerName { get; set; }
        public string CustomerKey { get; set; }
        public int IsDistributor { get; set; }
        public decimal? NetRevenue { get; set; }
        public int InvalidTax { get; set; }
        public decimal Revenue => NetRevenue ?? throw new InvalidOperationException("Chybí tržba objednávky.");
    }

    public sealed class BulletinItem
    {
        public long Id { get; set; }
        public long PurchaseOrderId { get; set; }
        public int? ProductId { get; set; }
        public int? ErpId { get; set; }
        public string ErpProductId { get; set; }
        public string Name { get; set; }
        public decimal Quantity { get; set; }
        public decimal TaxedPrice { get; set; }
        public string Key => ProductId.HasValue ? "product:" + ProductId.Value
            : !string.IsNullOrWhiteSpace(ErpProductId) ? "erp:" + ErpId + ":" + ErpProductId : "name:" + Name;
    }

    public sealed class BulletinFirstPurchase
    {
        public string CustomerKey { get; set; }
        public DateTime FirstPurchase { get; set; }
    }

    public sealed class BulletinSalesRep
    {
        public string ErpUid { get; set; }
        public string Name { get; set; }
    }
}
