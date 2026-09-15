using System;
using System.Collections.Generic;
using System.Linq;
using DocGen;

namespace Elsa.Jobs.BulletinGeneration
{
    public sealed class CustomerLifecycleChapter : IDocumentChapter
    {
        private readonly BulletinDataRepository _data;
        public CustomerLifecycleChapter(BulletinDataRepository data) { _data = data; }

        public DocumentChapter Build(DateTime generatedAt)
        {
            var period = new BulletinPeriod(generatedAt);
            var orders = _data.Orders(generatedAt);
            var first = _data.FirstPurchases(generatedAt);
            var current = Calculate(orders, first, period.Start, period.End);
            var previous = Calculate(orders, first, period.PreviousStart, period.PreviousEnd);
            var table = BulletinFormat.Table("Kdo přináší tržby",
                "Nový = první dokončený nákup v dostupné historii připadá do období. Vracející se = nákup v období a alespoň jeden před jeho začátkem. "
                    + "Odmlčený = nakupoval v předchozích třech měsících, ale ve sledovaných třech měsících už ne; nejde o definitivně ztraceného zákazníka. "
                    + "Identita: ERP UID, při jeho absenci e-mail. Změna těchto údajů může zákazníka rozdělit. Objednávky bez identity jsou vykázány zvlášť. "
                    + "Částky v Kč bez DPH, dopravy a platby; součet tržeb nových, vracejících se a bez identity odpovídá celkovým tržbám období.",
                "Skupina", "Zákazníci před rokem", "Tržby před rokem", "Zákazníci nyní", "Tržby nyní", "Trend tržeb");
            AddRow(table, "Noví", previous.NewCount, previous.NewRevenue, current.NewCount, current.NewRevenue);
            AddRow(table, "Vracející se", previous.ReturningCount, previous.ReturningRevenue, current.ReturningCount, current.ReturningRevenue);
            var lost = BulletinFormat.Row("Odmlčení", previous.LostCount.ToString(), "0", current.LostCount.ToString(), "0", "—");
            lost.Cells[2].Note = "Před odmlčením: " + BulletinFormat.Money(previous.LostPreviousRevenue);
            lost.Cells[4].Note = "Před odmlčením: " + BulletinFormat.Money(current.LostPreviousRevenue);
            table.Rows.Add(lost);
            table.Rows.Add(BulletinFormat.Row("Objednávky bez identity zákazníka", "—", BulletinFormat.Number(previous.UnidentifiedRevenue), "—",
                BulletinFormat.Number(current.UnidentifiedRevenue), BulletinFormat.Change(current.UnidentifiedRevenue, previous.UnidentifiedRevenue)));
            AddRow(table, "Celkem aktivní", previous.NewCount + previous.ReturningCount, previous.TotalRevenue,
                current.NewCount + current.ReturningCount, current.TotalRevenue);
            table.Rows.Last().Highlighted = true;
            var share = current.TotalRevenue == 0 ? "—" : BulletinFormat.Number(current.NewRevenue / current.TotalRevenue * 100) + "%";
            return new DocumentChapter
            {
                Title = "Noví, vracející se a odmlčení zákazníci",
                Text = "Období " + period.Label + " oproti " + period.PreviousLabel + ". Noví zákazníci přinesli " + BulletinFormat.Money(current.NewRevenue)
                    + " (" + share + " tržeb), vracející se " + BulletinFormat.Money(current.ReturningRevenue) + ". "
                    + current.LostCount + " zákazníků po nákupu v předchozích třech měsících zatím znovu nenakoupilo. "
                    + "Bez identity zůstává " + current.UnidentifiedOrders + " objednávek.",
                Table = table
            };
        }

        public static CustomerLifecycleStats Calculate(IEnumerable<BulletinOrder> source, IReadOnlyDictionary<string, DateTime> firstPurchases, DateTime start, DateTime end)
        {
            var orders = source.Where(o => o.PurchaseDate >= start.AddMonths(-3) && o.PurchaseDate < end).ToList();
            var current = orders.Where(o => o.PurchaseDate >= start).ToList();
            var known = current.Where(o => !string.IsNullOrEmpty(o.CustomerKey)).GroupBy(o => o.CustomerKey, StringComparer.OrdinalIgnoreCase).ToList();
            var activeKeys = new HashSet<string>(known.Select(g => g.Key), StringComparer.OrdinalIgnoreCase);
            var result = new CustomerLifecycleStats
            {
                TotalRevenue = current.Sum(o => o.Revenue),
                UnidentifiedOrders = current.Count(o => string.IsNullOrEmpty(o.CustomerKey)),
                UnidentifiedRevenue = current.Where(o => string.IsNullOrEmpty(o.CustomerKey)).Sum(o => o.Revenue)
            };
            foreach (var group in known)
            {
                if (!firstPurchases.TryGetValue(group.Key, out var first))
                    throw new InvalidOperationException("Chybí historie prvního nákupu zákazníka.");
                if (first >= start) { result.NewCount++; result.NewRevenue += group.Sum(o => o.Revenue); }
                else { result.ReturningCount++; result.ReturningRevenue += group.Sum(o => o.Revenue); }
            }
            var lost = orders.Where(o => o.PurchaseDate < start && !string.IsNullOrEmpty(o.CustomerKey) && !activeKeys.Contains(o.CustomerKey))
                .GroupBy(o => o.CustomerKey, StringComparer.OrdinalIgnoreCase).ToList();
            result.LostCount = lost.Count;
            result.LostPreviousRevenue = lost.Sum(g => g.Sum(o => o.Revenue));
            return result;
        }

        private static void AddRow(DocumentTable table, string title, int beforeCount, decimal beforeRevenue, int count, decimal revenue)
            => table.Rows.Add(BulletinFormat.Row(title, beforeCount.ToString(), BulletinFormat.Number(beforeRevenue), count.ToString(),
                BulletinFormat.Number(revenue), BulletinFormat.Change(revenue, beforeRevenue)));
    }

    public sealed class CustomerLifecycleStats
    {
        public int NewCount { get; set; }
        public decimal NewRevenue { get; set; }
        public int ReturningCount { get; set; }
        public decimal ReturningRevenue { get; set; }
        public int LostCount { get; set; }
        public decimal LostPreviousRevenue { get; set; }
        public int UnidentifiedOrders { get; set; }
        public decimal UnidentifiedRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
