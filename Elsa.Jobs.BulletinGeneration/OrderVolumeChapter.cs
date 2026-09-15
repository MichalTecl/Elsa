using System;
using System.Linq;
using DocGen;

namespace Elsa.Jobs.BulletinGeneration
{
    public sealed class OrderVolumeChapter : IDocumentChapter
    {
        private readonly BulletinDataRepository _data;
        public OrderVolumeChapter(BulletinDataRepository data) { _data = data; }
        public DocumentChapter Build(DateTime generatedAt)
        {
            var end = new DateTime(generatedAt.Year, generatedAt.Month, 1);
            var orders = _data.Orders(generatedAt);
            var lookup = orders.ToLookup(o => new DateTime(o.PurchaseDate.Year, o.PurchaseDate.Month, 1));
            var table = BulletinFormat.Table("Počty a průměrná hodnota objednávek",
                "Posledních 12 dokončených měsíců. Průměr = tržby / počet objednávek ve stavu 5; Kč bez DPH, dopravy a platby, po slevách. "
                + "Bez objednávek nelze průměr určit. VO znamená alespoň jednoho zákazníka s odpovídajícím ERP UID a příznakem distributora.",
                "Měsíc / kanál", "Objednávky před rokem", "Objednávky nyní", "Trend počtu", "Průměr před rokem", "Průměr nyní", "Trend průměru", "Trend tržeb");
            string summary = "";
            for (var offset = 1; offset <= 12; offset++)
            {
                var month = end.AddMonths(-offset);
                foreach (var channel in new[] { 1, 0 })
                {
                    var previous = lookup[month.AddYears(-1)].Where(o => o.IsDistributor == channel).ToList();
                    var current = lookup[month].Where(o => o.IsDistributor == channel).ToList();
                    var previousRevenue = previous.Sum(o => o.Revenue);
                    var currentRevenue = current.Sum(o => o.Revenue);
                    var previousAverage = previous.Count == 0 ? (decimal?)null : previousRevenue / previous.Count;
                    var currentAverage = current.Count == 0 ? (decimal?)null : currentRevenue / current.Count;
                    var name = channel == 1 ? "VO" : "MO";
                    table.Rows.Add(BulletinFormat.Row(month.ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("cs-CZ")) + " / " + name,
                        previous.Count.ToString(), current.Count.ToString(), BulletinFormat.Change(current.Count, previous.Count),
                        previousAverage.HasValue ? BulletinFormat.Number(previousAverage.Value) : "—",
                        currentAverage.HasValue ? BulletinFormat.Number(currentAverage.Value) : "—",
                        previousAverage.HasValue && currentAverage.HasValue ? BulletinFormat.Change(currentAverage.Value, previousAverage.Value) : "—",
                        BulletinFormat.Change(currentRevenue, previousRevenue)));
                    if (offset == 1)
                        summary += name + ": " + current.Count + " objednávek (" + BulletinFormat.Change(current.Count, previous.Count)
                            + "), průměr " + (currentAverage.HasValue ? BulletinFormat.Money(currentAverage.Value) : "—") + ". ";
                }
            }
            return new DocumentChapter
            {
                Title = "Objednávek je méně, nebo jsou menší?",
                Text = "Poslední dokončený měsíc: " + end.AddMonths(-1).ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("cs-CZ"))
                    + ". " + summary + "Počet objednávek a jejich průměrná hodnota společně vysvětlují změnu tržeb; jejich procentní změny nelze jednoduše sčítat.",
                Table = table
            };
        }
    }
}
