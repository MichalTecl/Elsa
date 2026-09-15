using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using Elsa.Common.Interfaces;
using DocGen;
using Robowire.RobOrm.Core;

namespace Elsa.Jobs.BulletinGeneration
{
    public sealed class RevenueChapter : IDocumentChapter
    {
        private const int MONTH_COUNT = 12;
        private readonly IDatabase _database;
        private readonly ISession _session;

        public RevenueChapter(IDatabase database, ISession session)
        {
            _database = database;
            _session = session;
        }

        public DocumentChapter Build(DateTime generatedAt)
        {
            var currentMonth = new DateTime(generatedAt.Year, generatedAt.Month, 1);
            var firstMonth = currentMonth.AddMonths(1 - MONTH_COUNT);
            var data = _database.Sql().ExecuteWithParams(@"SELECT
                YEAR(po.PurchaseDate) AS [Year],
                MONTH(po.PurchaseDate) AS [Month],
                SUM(CASE WHEN ISNULL(c.IsDistributor, 0) = 0 THEN revenue.NetRevenue ELSE 0 END) AS Retail,
                SUM(CASE WHEN c.IsDistributor = 1 THEN revenue.NetRevenue ELSE 0 END) AS Wholesale,
                SUM(CASE WHEN firstItem.TaxPercent IS NULL OR firstItem.TaxPercent < 0 THEN 1 ELSE 0 END) AS InvalidTaxCount
            " + BulletinQueries.OrderFrom + @"
            WHERE po.OrderStatusId = 5
              AND po.ProjectId = {0}
              AND po.PurchaseDate >= {1}
              AND po.PurchaseDate < {2}
            GROUP BY YEAR(po.PurchaseDate), MONTH(po.PurchaseDate)
            ORDER BY [Year], [Month];", _session.Project.Id, firstMonth.AddYears(-1), generatedAt)
                .AutoMap<RevData>()
                .ToDictionary(row => new DateTime(row.Year, row.Month, 1));

            if (data.Values.Any(row => row.InvalidTaxCount > 0))
                throw new InvalidOperationException("Nelze určit sazbu DPH z první položky některých objednávek.");

            var chart = new GroupedStackedChart { Title = "Měsíční tržby — meziroční srovnání", Unit = "Kč" };
            chart.Segments.Add(new ChartSegment { Label = "Maloobchod", Color = "#38A89D" });
            chart.Segments.Add(new ChartSegment { Label = "Velkoobchod", Color = "#5367C8" });

            var elapsedFraction = (decimal)(generatedAt - currentMonth).Ticks
                / (currentMonth.AddMonths(1) - currentMonth).Ticks;

            for (var i = 0; i < MONTH_COUNT; i++)
            {
                var month = firstMonth.AddMonths(i);
                var previous = data.TryGetValue(month.AddYears(-1), out var previousData) ? previousData : new RevData();
                var actual = data.TryGetValue(month, out var currentData) ? currentData : new RevData();
                var group = new ChartGroup { Label = month.ToString("MMMM", CultureInfo.GetCultureInfo("cs-CZ")) };
                group.Columns.Add(new ChartColumn { Label = month.AddYears(-1).Year.ToString(), Values = new[] { previous.Retail, previous.Wholesale }, SegmentKeys = RevenueKeys(month.AddYears(-1)) });
                var current = new ChartColumn
                {
                    Label = month.Year.ToString(),
                    Values = new[] { actual.Retail, actual.Wholesale },
                    SegmentKeys = RevenueKeys(month)
                };
                if (month == currentMonth)
                {
                    // At the exact start of the month there is no basis for an estimate.
                    if (elapsedFraction > 0)
                    {
                        current.ProjectedAdditions = new[]
                        {
                            Math.Round(current.Values[0] * (1 - elapsedFraction) / elapsedFraction, 2),
                            Math.Round(current.Values[1] * (1 - elapsedFraction) / elapsedFraction, 2)
                        };
                    }
                }
                if (month < currentMonth)
                {
                    current.SegmentLabels = new[]
                    {
                        FormatChange(actual.Retail, previous.Retail),
                        FormatChange(actual.Wholesale, previous.Wholesale)
                    };
                }
                group.Columns.Add(current);
                chart.Groups.Add(group);
            }

            return new DocumentChapter
            {
                Title = "Jak se vyvíjejí naše tržby",
                Text = "Tržby po slevách, bez DPH, dopravy a poplatků za platbu, podle data nákupu.\n"
                    + "Graf zobrazuje dvanáct měsíců včetně aktuálního. V každé dvojici je vlevo stejný měsíc předchozího roku "
                    + "a vpravo sledovaný měsíc. Barevná patra rozlišují maloobchod a velkoobchod. Procenta ukazují změnu proti stejnému měsíci loni.\n"
                    + "Syté barvy zobrazují dosavadní tržby. Světlá patra aktuálního měsíce jsou odhadem dalších tržeb "
                    + "do jeho konce při zachování dosavadního tempa. Odhad vychází z uplynulé části měsíce k "
                    + generatedAt.ToString("d. M. yyyy HH:mm", CultureInfo.GetCultureInfo("cs-CZ")) + ". "
                    + "Na úplném začátku měsíce se odhad nezobrazuje.",
                Chart = chart,
                Table = CreateTable(chart, currentMonth)
            };
        }

        private static DocumentTable CreateTable(GroupedStackedChart chart, DateTime currentMonth)
        {
            var culture = CultureInfo.GetCultureInfo("cs-CZ");
            var table = new DocumentTable
            {
                Title = "Měsíční přehled tržeb",
                Note = "Částky jsou v Kč bez DPH, dopravy a poplatků za platbu, po započtení slev. "
                    + "Každý řádek porovnává sledovaný měsíc se stejným měsícem o rok dříve. Přesný měsíc a rok se zobrazí po najetí na buňku. "
                    + "Aktuální měsíc obsahuje dosavadní tržby. Trend se u něj neuvádí; pomlčka označuje také srovnání s nulovou loňskou tržbou."
            };
            table.Columns.AddRange(new[]
            {
                "Měsíc", "VO před rokem", "MO před rokem", "Celkem před rokem",
                "VO sledovaný měsíc", "MO sledovaný měsíc", "Celkem sledovaný měsíc", "Trend MO", "Trend VO", "Trend Celkem"
            });
            for (var offset = 0; offset < chart.Groups.Count; offset++)
            {
                var currentDate = currentMonth.AddMonths(-offset);
                var previousDate = currentDate.AddYears(-1);
                var group = chart.Groups[chart.Groups.Count - 1 - offset];
                var previous = group.Columns[0];
                var current = group.Columns[1];
                var row = new DocumentTableRow { Highlighted = offset == 0 };
                row.Cells.Add(new DocumentTableCell
                {
                    Text = currentDate.ToString("MMMM", culture),
                    Tooltip = currentDate.ToString("MMMM yyyy", culture)
                });
                row.Cells.Add(AmountCell(previous.Values[1], previousDate, "vo"));
                row.Cells.Add(AmountCell(previous.Values[0], previousDate, "mo"));
                row.Cells.Add(AmountCell(previous.Values.Sum(), previousDate, null, true));
                row.Cells.Add(AmountCell(current.Values[1], currentDate, "vo"));
                row.Cells.Add(AmountCell(current.Values[0], currentDate, "mo"));
                row.Cells.Add(AmountCell(current.Values.Sum(), currentDate, null, true));
                row.Cells.Add(TrendCell(current.SegmentLabels?[0], currentDate, previousDate, "mo"));
                row.Cells.Add(TrendCell(current.SegmentLabels?[1], currentDate, previousDate, "vo"));
                row.Cells.Add(TrendCell(offset == 0 ? null : FormatChange(current.Values.Sum(), previous.Values.Sum()), currentDate, previousDate));
                table.Rows.Add(row);
            }
            return table;
        }

        private static DocumentTableCell AmountCell(decimal amount, DateTime month, string channel, bool emphasized = false)
        {
            var culture = CultureInfo.GetCultureInfo("cs-CZ");
            return new DocumentTableCell
            {
                Text = amount.ToString("N0", culture),
                Tooltip = month.ToString("MMMM yyyy", culture),
                Emphasized = emphasized,
                LinkKey = channel == null ? null : RevenueKey(month, channel),
                HighlightTargets = channel == null ? RevenueKeys(month) : null
            };
        }

        private static DocumentTableCell TrendCell(string trend, DateTime currentMonth, DateTime previousMonth, string channel = null)
        {
            var culture = CultureInfo.GetCultureInfo("cs-CZ");
            return new DocumentTableCell
            {
                Text = trend ?? "—",
                Tooltip = currentMonth.ToString("MMMM yyyy", culture) + " oproti " + previousMonth.ToString("MMMM yyyy", culture),
                Emphasized = true,
                IsPositive = trend != null && trend.StartsWith("+", StringComparison.Ordinal),
                HighlightTrends = true,
                HighlightTargets = trend == null ? null : channel == null ? RevenueKeys(currentMonth) : new[] { RevenueKey(currentMonth, channel) }
            };
        }

        private static string RevenueKey(DateTime month, string channel)
            => "revenue-" + month.ToString("yyyy-MM", CultureInfo.InvariantCulture) + "-" + channel;

        private static string[] RevenueKeys(DateTime month)
            => new[] { RevenueKey(month, "mo"), RevenueKey(month, "vo") };

        private static string FormatChange(decimal current, decimal previous)
        {
            if (previous == 0) return null;

            var percent = Math.Round((current - previous) / previous * 100, 0, MidpointRounding.AwayFromZero);
            return percent.ToString("+0;-0;0", CultureInfo.InvariantCulture) + "%";
        }

        public class RevData
        {
            public int Year { get; set; }
            public int Month { get; set; }
            public decimal Retail { get; set; }
            public decimal Wholesale { get; set; }
            public int InvalidTaxCount { get; set; }
        }
    }
}
