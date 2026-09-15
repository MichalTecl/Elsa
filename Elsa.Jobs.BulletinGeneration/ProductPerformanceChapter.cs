using System;
using System.Collections.Generic;
using System.Linq;
using DocGen;

namespace Elsa.Jobs.BulletinGeneration
{
    public sealed class ProductPerformanceChapter : IDocumentChapter
    {
        private readonly BulletinDataRepository _data;
        public ProductPerformanceChapter(BulletinDataRepository data) { _data = data; }

        public DocumentChapter Build(DateTime generatedAt)
        {
            var period = new BulletinPeriod(generatedAt);
            var orders = _data.Orders(generatedAt).Where(o => period.Contains(o.PurchaseDate) || period.ContainsPrevious(o.PurchaseDate)).ToList();
            var products = Calculate(orders, _data.Items(generatedAt), period);
            var winners = products.Where(p => p.Delta > 0).OrderByDescending(p => p.Delta).ThenBy(p => p.Name).Take(10).ToList();
            var losers = products.Where(p => p.Delta < 0).OrderBy(p => p.Delta).ThenBy(p => p.Name).Take(10).ToList();
            var selected = winners.Concat(losers).ToList();
            var before = products.Sum(p => p.PreviousRevenue);
            var now = products.Sum(p => p.CurrentRevenue);
            var change = now - before;
            var contributors = (change < 0 ? losers : winners).Take(3).ToList();
            var table = BulletinFormat.Table("Výrobky s největším růstem a poklesem",
                "Nejvýše 10 růstů a 10 poklesů; ostatní výrobky jsou sečteny na konci. Částky v Kč bez DPH, dopravy a platby. "
                + "Konečná tržba objednávky po slevách je rozdělena poměrně podle původních položkových cen s DPH. "
                + "Jde o alokaci slev, nikoli přesnou historii slev jednotlivých výrobků. Sady se počítají jako prodaná sada, bez skladových podčástí.",
                "Výrobek", "Tržby před rokem", "Tržby nyní", "Změna Kč", "Změna %", "Množství před rokem", "Množství nyní", "Kč / jednotku před rokem", "Kč / jednotku nyní");
            foreach (var product in selected) AddRow(table, product);
            var remaining = products.Except(selected).ToList();
            if (remaining.Any()) AddRow(table, Sum("Ostatní výrobky", remaining));
            var total = Sum("Celkem", products);
            AddRow(table, total);
            table.Rows.Last().Highlighted = true;
            var contribution = contributors.Any()
                ? " Největší příspěvky: " + string.Join(", ", contributors.Select(p => p.Name + " (" + BulletinFormat.Signed(p.Delta) + " Kč)"))
                    + ". Jejich součet je " + BulletinFormat.Signed(contributors.Sum(p => p.Delta)) + " Kč."
                : "";
            return new DocumentChapter
            {
                Title = "Co táhne růst a co padá",
                Text = "Období " + period.Label + " oproti " + period.PreviousLabel + ". Tržby se změnily z "
                    + BulletinFormat.Money(before) + " na " + BulletinFormat.Money(now) + ", tedy o " + BulletinFormat.Signed(change) + " Kč ("
                    + BulletinFormat.Change(now, before) + ")." + contribution
                    + " Průměrná tržba na jednotku odráží ceny i slevy; sama o sobě nedokazuje zdražení.",
                Table = table
            };
        }

        public static List<ProductPerformance> Calculate(IEnumerable<BulletinOrder> orders, IEnumerable<BulletinItem> items, BulletinPeriod period)
        {
            var lookup = items.ToLookup(i => i.PurchaseOrderId);
            var products = new Dictionary<string, ProductPerformance>(StringComparer.Ordinal);
            foreach (var order in orders.OrderBy(o => o.PurchaseDate).ThenBy(o => o.Id))
            {
                var current = period.Contains(order.PurchaseDate);
                if (!current && !period.ContainsPrevious(order.PurchaseDate)) continue;
                var lines = lookup[order.Id].OrderBy(i => i.Id).ToList();
                var weight = lines.Sum(i => i.TaxedPrice);
                if (weight <= 0 || lines.Any(i => i.TaxedPrice < 0))
                {
                    Add(products, "unallocated", "Nepřiřazené tržby (chybí použitelná položková cena)", current, order.Revenue, 0);
                    continue;
                }
                decimal allocated = 0;
                for (var index = 0; index < lines.Count; index++)
                {
                    var line = lines[index];
                    var amount = index == lines.Count - 1 ? order.Revenue - allocated : order.Revenue * line.TaxedPrice / weight;
                    allocated += amount;
                    Add(products, line.Key, line.Name, current, amount, line.Quantity);
                }
            }
            return products.Values.ToList();
        }

        private static void Add(Dictionary<string, ProductPerformance> products, string key, string name, bool current, decimal revenue, decimal quantity)
        {
            if (!products.TryGetValue(key, out var product)) products.Add(key, product = new ProductPerformance());
            product.Name = name;
            if (current) { product.CurrentRevenue += revenue; product.CurrentQuantity += quantity; }
            else { product.PreviousRevenue += revenue; product.PreviousQuantity += quantity; }
        }
        private static ProductPerformance Sum(string name, IEnumerable<ProductPerformance> rows) => new ProductPerformance
        {
            Name = name, PreviousRevenue = rows.Sum(r => r.PreviousRevenue), CurrentRevenue = rows.Sum(r => r.CurrentRevenue),
            PreviousQuantity = rows.Sum(r => r.PreviousQuantity), CurrentQuantity = rows.Sum(r => r.CurrentQuantity), IsSummary = true
        };
        private static void AddRow(DocumentTable table, ProductPerformance p)
        {
            table.Rows.Add(BulletinFormat.Row(p.Name, BulletinFormat.Number(p.PreviousRevenue), BulletinFormat.Number(p.CurrentRevenue),
                BulletinFormat.Signed(p.Delta), BulletinFormat.Change(p.CurrentRevenue, p.PreviousRevenue),
                BulletinFormat.Quantity(p.PreviousQuantity), BulletinFormat.Quantity(p.CurrentQuantity),
                p.IsSummary || p.PreviousQuantity == 0 ? "—" : BulletinFormat.Number(p.PreviousRevenue / p.PreviousQuantity),
                p.IsSummary || p.CurrentQuantity == 0 ? "—" : BulletinFormat.Number(p.CurrentRevenue / p.CurrentQuantity)));
        }
    }

    public sealed class ProductPerformance
    {
        public string Name { get; set; }
        public decimal PreviousRevenue { get; set; }
        public decimal CurrentRevenue { get; set; }
        public decimal PreviousQuantity { get; set; }
        public decimal CurrentQuantity { get; set; }
        public bool IsSummary { get; set; }
        public decimal Delta => CurrentRevenue - PreviousRevenue;
    }
}
