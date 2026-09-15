using System;
using System.Collections.Generic;
using System.Linq;
using DocGen;

namespace Elsa.Jobs.BulletinGeneration
{
    public sealed class DormantWholesaleChapter : IDocumentChapter
    {
        private readonly BulletinDataRepository _data;
        public DormantWholesaleChapter(BulletinDataRepository data) { _data = data; }

        public DocumentChapter Build(DateTime generatedAt)
        {
            var candidates = Calculate(_data.Orders(generatedAt), generatedAt);
            var reps = _data.SalesReps(generatedAt).Where(r => !string.IsNullOrWhiteSpace(r.ErpUid))
                .ToLookup(r => r.ErpUid.Trim(), StringComparer.OrdinalIgnoreCase);
            var table = BulletinFormat.Table("VO zákazníci k prověření",
                "Heuristika z dokončených objednávek za posledních 24 měsíců: alespoň 4 různé objednací dny, "
                    + "nejméně 75 % intervalů mezi polovinou a dvojnásobkem mediánu. Upozornění po 1,5násobku mediánu, nejdříve po 14 dnech. "
                    + "Sezónnost může výsledek ovlivnit. Zobrazeno nejvýše 30 zákazníků podle tržeb za posledních 12 měsíců. "
                    + "Obchodní zástupci odpovídají aktuálně platným přiřazením. Částky v Kč bez DPH, dopravy a platby.",
                "Zákazník / ERP UID", "Poslední objednávka", "Dní od nákupu", "Obvyklý interval (dní)", "Dní nad hranicí", "Objednací dny (24m)", "Tržby (12m)", "Obchodní zástupce");
            foreach (var candidate in candidates.Take(30))
            {
                var names = reps[candidate.ErpUid].Select(r => r.Name).Distinct().OrderBy(n => n).ToArray();
                table.Rows.Add(BulletinFormat.Row(candidate.Name + " / " + candidate.ErpUid,
                    candidate.LastPurchase.ToString("d. M. yyyy"), candidate.DaysSince.ToString(),
                    BulletinFormat.Quantity(candidate.MedianDays), BulletinFormat.Number(candidate.DaysOverdue),
                    candidate.OrderDays.ToString(), BulletinFormat.Number(candidate.RevenueLastYear),
                    names.Length == 0 ? "Nepřiřazen" : string.Join(", ", names)));
            }
            return new DocumentChapter
            {
                Title = "Velkoobchodní zákazníci, kteří se odmlčeli",
                Text = candidates.Count == 0 ? "Žádný VO zákazník nyní nesplňuje níže popsaná pravidla pro překročení obvyklého intervalu."
                    : candidates.Count + " VO zákazníků překročilo obvyklý interval nákupů. Za posledních 12 měsíců přinesli dohromady "
                        + BulletinFormat.Money(candidates.Sum(c => c.RevenueLastYear)) + ". Jde o podnět k prověření, ne potvrzení ztráty zákazníka.",
                Table = table
            };
        }

        public static List<DormantWholesaleCustomer> Calculate(IEnumerable<BulletinOrder> source, DateTime generatedAt)
        {
            var orders = source.Where(o => o.PurchaseDate >= generatedAt.AddYears(-2) && o.PurchaseDate < generatedAt
                && o.IsDistributor == 1 && !string.IsNullOrWhiteSpace(o.CustomerErpUid));
            var result = new List<DormantWholesaleCustomer>();
            foreach (var group in orders.GroupBy(o => o.CustomerErpUid.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                var dates = group.Select(o => o.PurchaseDate.Date).Distinct().OrderBy(d => d).ToArray();
                if (dates.Length < 4) continue;
                var intervals = dates.Skip(1).Select((date, i) => (decimal)(date - dates[i]).TotalDays).OrderBy(d => d).ToArray();
                var median = intervals.Length % 2 == 1 ? intervals[intervals.Length / 2]
                    : (intervals[intervals.Length / 2 - 1] + intervals[intervals.Length / 2]) / 2;
                if (intervals.Count(d => d >= median / 2 && d <= median * 2) < Math.Ceiling(intervals.Length * 0.75m)) continue;
                var latest = group.OrderByDescending(o => o.PurchaseDate).ThenByDescending(o => o.Id).First();
                var daysSince = (generatedAt.Date - latest.PurchaseDate.Date).Days;
                var threshold = Math.Max(14, median * 1.5m);
                if (daysSince <= threshold) continue;
                result.Add(new DormantWholesaleCustomer
                {
                    Name = string.IsNullOrWhiteSpace(latest.CustomerName) ? "Bez názvu" : latest.CustomerName,
                    ErpUid = group.Key, LastPurchase = latest.PurchaseDate, DaysSince = daysSince,
                    MedianDays = median, DaysOverdue = daysSince - threshold, OrderDays = dates.Length,
                    RevenueLastYear = group.Where(o => o.PurchaseDate >= generatedAt.AddYears(-1)).Sum(o => o.Revenue)
                });
            }
            return result.OrderByDescending(c => c.RevenueLastYear).ThenByDescending(c => c.DaysOverdue).ThenBy(c => c.ErpUid).ToList();
        }
    }

    public sealed class DormantWholesaleCustomer
    {
        public string Name { get; set; }
        public string ErpUid { get; set; }
        public DateTime LastPurchase { get; set; }
        public int DaysSince { get; set; }
        public decimal MedianDays { get; set; }
        public decimal DaysOverdue { get; set; }
        public int OrderDays { get; set; }
        public decimal RevenueLastYear { get; set; }
    }
}
