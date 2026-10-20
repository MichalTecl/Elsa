using Elsa.App.OrdersInfo.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Elsa.App.OrdersInfo.App.Aggregations
{
    /// <summary>
    /// Distribution of orders in time: columns are consecutive unique periods (weeks, days, months, years),
    /// rows are repeating time slots (days of week, hours, hours of days of week).
    /// </summary>
    public class OrdersTimeDistributionAggregation : IOrdersDrilldownAggregator
    {
        private const string METRIC_PARAM = "metric";
        private const string COLUMNS_PARAM = "columns";
        private const string ROWS_PARAM = "rows";

        private const string METRIC_ORDERS_COUNT = "Počet objednávek";
        private const string METRIC_REVENUE = "Tržby";
        private const string METRIC_ORDERS_COUNT_SHARE = "Podíl počtu objednávek ve sloupci (%)";
        private const string METRIC_REVENUE_SHARE = "Podíl tržeb ve sloupci (%)";

        private const string COLUMNS_WEEKS = "Kalendářní týdny";
        private const string COLUMNS_DAYS = "Dny";
        private const string COLUMNS_MONTHS = "Kalendářní měsíce";
        private const string COLUMNS_YEARS = "Kalendářní roky";

        private const string ROWS_DAYS_OF_WEEK = "Dny v týdnu";
        private const string ROWS_HOURS_AND_DAYS = "Hodiny a dny";
        private const string ROWS_HOURS = "Hodiny";
        private const string ROWS_MONTHS = "Kalendářní měsíce";

        private const int MAX_COLUMNS = 50;

        private static readonly CultureInfo _czechCulture = CultureInfo.GetCultureInfo("cs-CZ");

        // Monday first
        private static readonly string[] _dayNames = { "Pondělí", "Úterý", "Středa", "Čtvrtek", "Pátek", "Sobota", "Neděle" };
        private static readonly string[] _dayShortNames = { "Po", "Út", "St", "Čt", "Pá", "So", "Ne" };

        private static readonly string[] _monthNames =
        {
            "Leden", "Únor", "Březen", "Duben", "Květen", "Červen",
            "Červenec", "Srpen", "Září", "Říjen", "Listopad", "Prosinec"
        };

        public List<OrderAggregatorParamModel> Parameters { get; set; } = new List<OrderAggregatorParamModel>
        {
            new OrderAggregatorParamModel
            {
                OptionId = METRIC_PARAM,
                Title = "Metrika",
                Values = new List<string> { METRIC_ORDERS_COUNT, METRIC_REVENUE, METRIC_ORDERS_COUNT_SHARE, METRIC_REVENUE_SHARE }
            },
            new OrderAggregatorParamModel
            {
                OptionId = COLUMNS_PARAM,
                Title = "Sloupce",
                Values = new List<string> { COLUMNS_WEEKS, COLUMNS_DAYS, COLUMNS_MONTHS, COLUMNS_YEARS }
            },
            new OrderAggregatorParamModel
            {
                OptionId = ROWS_PARAM,
                Title = "Řádky",
                Values = new List<string> { ROWS_DAYS_OF_WEEK, ROWS_HOURS_AND_DAYS, ROWS_HOURS, ROWS_MONTHS }
            }
        };

        public AggregatedResultModel Process(AggregationSourceOrders sourceOrders, Dictionary<string, string> selectedParameters)
        {
            var metric = selectedParameters[METRIC_PARAM];
            var columnsMode = selectedParameters[COLUMNS_PARAM];
            var rowsMode = selectedParameters[ROWS_PARAM];

            var buckets = sourceOrders.Sql(@"
                SELECT CAST(po.PurchaseDate AS date) AS PurchaseDay,
                       DATEPART(hour, po.PurchaseDate) AS PurchaseHour,
                       COUNT(*) AS OrdersCount,
                       SUM(po.PriceWithVat) AS Revenue
                  FROM PurchaseOrder po
                 WHERE po.Id IN ({sourceOrdersQuery})
                 GROUP BY CAST(po.PurchaseDate AS date), DATEPART(hour, po.PurchaseDate)")
                .MapRows(r => new HourBucket
                {
                    Day = r.GetDateTime(0),
                    Hour = r.GetInt32(1),
                    OrdersCount = r.GetInt32(2),
                    Revenue = r.GetDecimal(3)
                });

            var model = new AggregatedResultModel
            {
                RowsTitle = GetRowsTitle(rowsMode),
                Legend = CreateLegend(metric, columnsMode, rowsMode)
            };

            if (buckets.Count == 0)
                return model;

            var periods = GetPeriods(columnsMode, buckets.Min(b => b.Day), buckets.Max(b => b.Day));
            if (periods.Count > MAX_COLUMNS)
            {
                model.Message = $"Výsledek by měl {periods.Count} sloupců, zobrazit lze nejvýše {MAX_COLUMNS}. " +
                                "Zužte období filtrem data nákupu nebo zvolte delší časové úseky pro sloupce.";
                return model;
            }

            var rows = GetRows(rowsMode);

            model.DefineColumns(periods.Select(p => p.Title));
            model.DefineRows(rows);

            var periodTitleByStart = periods.ToDictionary(p => p.Start, p => p.Title);
            var counts = new Dictionary<string, int>();
            var revenues = new Dictionary<string, decimal>();

            foreach (var bucket in buckets)
            {
                var key = CellKey(
                    GetRowTitle(rowsMode, bucket.Day, bucket.Hour),
                    periodTitleByStart[GetPeriodStart(columnsMode, bucket.Day)]);

                counts.TryGetValue(key, out var count);
                counts[key] = count + bucket.OrdersCount;

                revenues.TryGetValue(key, out var revenue);
                revenues[key] = revenue + bucket.Revenue;
            }

            var useRevenue = metric == METRIC_REVENUE || metric == METRIC_REVENUE_SHARE;
            var isShare = metric == METRIC_ORDERS_COUNT_SHARE || metric == METRIC_REVENUE_SHARE;

            decimal GetCellAmount(string row, string column)
            {
                var key = CellKey(row, column);

                if (useRevenue)
                    return revenues.TryGetValue(key, out var revenue) ? revenue : 0m;

                return counts.TryGetValue(key, out var count) ? count : 0;
            }

            foreach (var period in periods)
            {
                var columnTotal = rows.Sum(row => GetCellAmount(row, period.Title));

                for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    var row = rows[rowIndex];
                    var amount = GetCellAmount(row, period.Title);
                    string value;

                    if (!isShare)
                        value = amount.ToString(useRevenue ? "N2" : "N0", _czechCulture);
                    else if (columnTotal == 0m)
                        value = "–";
                    else
                        value = (amount * 100m / columnTotal).ToString("N1", _czechCulture) + " %";

                    model.Write(row, period.Title, value);

                    if (counts.ContainsKey(CellKey(row, period.Title)))
                        model.SetOrdersDrilldown(row, period.Title, CreateDrilldownKey(period.Start, rowIndex));
                }
            }

            return model;
        }

        public IList<long> GetDrilldownOrderIds(AggregationSourceOrders sourceOrders, Dictionary<string, string> selectedParameters, string drilldownKey)
        {
            var columnsMode = selectedParameters[COLUMNS_PARAM];
            var rowsMode = selectedParameters[ROWS_PARAM];

            ParseDrilldownKey(drilldownKey, out var periodStart, out var rowIndex);

            if (periodStart != GetPeriodStart(columnsMode, periodStart))
                throw new ArgumentException($"Neplatný začátek období v odkazu: {drilldownKey}");

            if (rowIndex < 0 || rowIndex >= GetRows(rowsMode).Count)
                throw new ArgumentException($"Neplatný řádek v odkazu: {drilldownKey}");

            const string sqlBase = @"
                SELECT po.Id
                  FROM PurchaseOrder po
                 WHERE po.Id IN ({sourceOrdersQuery})
                   AND po.PurchaseDate >= {0}
                   AND po.PurchaseDate < {1}
                   AND ";

            // day of week index, 0 = Monday, independent on DATEFIRST
            const string dayOfWeekSql = "(DATEDIFF(day, '19000101', po.PurchaseDate) % 7)";

            var periodEnd = GetNextPeriodStart(columnsMode, periodStart);

            switch (rowsMode)
            {
                case ROWS_DAYS_OF_WEEK:
                    return sourceOrders.Sql(sqlBase + dayOfWeekSql + " = {2}", periodStart, periodEnd, rowIndex)
                        .MapRows(r => r.GetInt64(0));
                case ROWS_HOURS_AND_DAYS:
                    return sourceOrders.Sql(sqlBase + dayOfWeekSql + " = {2} AND DATEPART(hour, po.PurchaseDate) = {3}",
                            periodStart, periodEnd, rowIndex / 24, rowIndex % 24)
                        .MapRows(r => r.GetInt64(0));
                case ROWS_HOURS:
                    return sourceOrders.Sql(sqlBase + "DATEPART(hour, po.PurchaseDate) = {2}", periodStart, periodEnd, rowIndex)
                        .MapRows(r => r.GetInt64(0));
                case ROWS_MONTHS:
                    return sourceOrders.Sql(sqlBase + "MONTH(po.PurchaseDate) = {2}", periodStart, periodEnd, rowIndex + 1)
                        .MapRows(r => r.GetInt64(0));
                default:
                    throw new ArgumentException($"Neznámý typ řádků {rowsMode}");
            }
        }

        private static string CreateDrilldownKey(DateTime periodStart, int rowIndex)
        {
            return $"{periodStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}|{rowIndex.ToString(CultureInfo.InvariantCulture)}";
        }

        private static void ParseDrilldownKey(string drilldownKey, out DateTime periodStart, out int rowIndex)
        {
            var parts = (drilldownKey ?? string.Empty).Split('|');

            if (parts.Length != 2
                || !DateTime.TryParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out periodStart)
                || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out rowIndex))
            {
                throw new ArgumentException($"Neplatný odkaz na buňku agregace: {drilldownKey}");
            }
        }

        private static string CellKey(string row, string column)
        {
            return row + "\u001F" + column;
        }

        private static int GetDayOfWeekIndex(DateTime day)
        {
            return ((int)day.DayOfWeek + 6) % 7;
        }

        private static string FormatHour(int hour)
        {
            return $"{hour:00}:00–{hour:00}:59";
        }

        private static List<string> GetRows(string rowsMode)
        {
            switch (rowsMode)
            {
                case ROWS_DAYS_OF_WEEK:
                    return _dayNames.ToList();
                case ROWS_HOURS_AND_DAYS:
                    return _dayNames.SelectMany(day => Enumerable.Range(0, 24).Select(hour => $"{day} {FormatHour(hour)}")).ToList();
                case ROWS_HOURS:
                    return Enumerable.Range(0, 24).Select(FormatHour).ToList();
                case ROWS_MONTHS:
                    return _monthNames.ToList();
                default:
                    throw new ArgumentException($"Neznámý typ řádků {rowsMode}");
            }
        }

        private static string GetRowTitle(string rowsMode, DateTime day, int hour)
        {
            switch (rowsMode)
            {
                case ROWS_DAYS_OF_WEEK:
                    return _dayNames[GetDayOfWeekIndex(day)];
                case ROWS_HOURS_AND_DAYS:
                    return $"{_dayNames[GetDayOfWeekIndex(day)]} {FormatHour(hour)}";
                case ROWS_HOURS:
                    return FormatHour(hour);
                case ROWS_MONTHS:
                    return _monthNames[day.Month - 1];
                default:
                    throw new ArgumentException($"Neznámý typ řádků {rowsMode}");
            }
        }

        private static string GetRowsTitle(string rowsMode)
        {
            switch (rowsMode)
            {
                case ROWS_DAYS_OF_WEEK:
                    return "Den v týdnu";
                case ROWS_HOURS_AND_DAYS:
                    return "Den v týdnu a hodina";
                case ROWS_HOURS:
                    return "Hodina";
                case ROWS_MONTHS:
                    return "Měsíc";
                default:
                    throw new ArgumentException($"Neznámý typ řádků {rowsMode}");
            }
        }

        private static DateTime GetPeriodStart(string columnsMode, DateTime day)
        {
            day = day.Date;

            switch (columnsMode)
            {
                case COLUMNS_WEEKS:
                    return day.AddDays(-GetDayOfWeekIndex(day));
                case COLUMNS_DAYS:
                    return day;
                case COLUMNS_MONTHS:
                    return new DateTime(day.Year, day.Month, 1);
                case COLUMNS_YEARS:
                    return new DateTime(day.Year, 1, 1);
                default:
                    throw new ArgumentException($"Neznámý typ sloupců {columnsMode}");
            }
        }

        private static DateTime GetNextPeriodStart(string columnsMode, DateTime periodStart)
        {
            switch (columnsMode)
            {
                case COLUMNS_WEEKS:
                    return periodStart.AddDays(7);
                case COLUMNS_DAYS:
                    return periodStart.AddDays(1);
                case COLUMNS_MONTHS:
                    return periodStart.AddMonths(1);
                case COLUMNS_YEARS:
                    return periodStart.AddYears(1);
                default:
                    throw new ArgumentException($"Neznámý typ sloupců {columnsMode}");
            }
        }

        private static string GetPeriodTitle(string columnsMode, DateTime periodStart)
        {
            switch (columnsMode)
            {
                case COLUMNS_WEEKS:
                    var periodEnd = periodStart.AddDays(6);
                    var startFormat = periodStart.Year == periodEnd.Year ? "d. M." : "d. M. yyyy";
                    return $"{periodStart.ToString(startFormat, _czechCulture)} - {periodEnd.ToString("d. M. yyyy", _czechCulture)}";
                case COLUMNS_DAYS:
                    return $"{_dayShortNames[GetDayOfWeekIndex(periodStart)]} {periodStart.ToString("d. M. yyyy", _czechCulture)}";
                case COLUMNS_MONTHS:
                    return $"{_monthNames[periodStart.Month - 1]} {periodStart.Year}";
                case COLUMNS_YEARS:
                    return periodStart.Year.ToString(CultureInfo.InvariantCulture);
                default:
                    throw new ArgumentException($"Neznámý typ sloupců {columnsMode}");
            }
        }

        private static List<Period> GetPeriods(string columnsMode, DateTime firstDay, DateTime lastDay)
        {
            var result = new List<Period>();
            var lastPeriodStart = GetPeriodStart(columnsMode, lastDay);

            for (var start = GetPeriodStart(columnsMode, firstDay);
                 start <= lastPeriodStart;
                 start = GetNextPeriodStart(columnsMode, start))
            {
                result.Add(new Period { Start = start, Title = GetPeriodTitle(columnsMode, start) });

                // protects against endless loop and huge allocations, the limit is checked by the caller
                if (result.Count > MAX_COLUMNS)
                    break;
            }

            if (result.Count > MAX_COLUMNS)
            {
                // count the rest without creating titles to report the real number of columns
                var start = GetNextPeriodStart(columnsMode, result.Last().Start);
                while (start <= lastPeriodStart)
                {
                    result.Add(new Period { Start = start });
                    start = GetNextPeriodStart(columnsMode, start);
                }
            }

            return result;
        }

        private static string CreateLegend(string metric, string columnsMode, string rowsMode)
        {
            const string revenueDefinition = "tržby, tj. součet celkových cen objednávek s DPH (včetně poštovného a uplatněných slev)";
            const string shareNote = " Každý sloupec dává dohromady 100 %, hodnoty jsou zaokrouhlené na desetiny procenta, proto se součet může nepatrně lišit. Pomlčka znamená, že ve sloupci nebyla žádná objednávka.";

            string metricText;
            switch (metric)
            {
                case METRIC_REVENUE:
                    metricText = $"Hodnoty jsou {revenueDefinition}.";
                    break;
                case METRIC_ORDERS_COUNT_SHARE:
                    metricText = "Hodnoty jsou podíly počtu objednávek v daném řádku na celkovém počtu objednávek ve sloupci." + shareNote;
                    break;
                case METRIC_REVENUE_SHARE:
                    metricText = $"Hodnoty jsou podíly tržeb v daném řádku na celkových tržbách ve sloupci; {revenueDefinition}." + shareNote;
                    break;
                default:
                    metricText = "Hodnoty jsou počty objednávek.";
                    break;
            }

            string columnsText;
            switch (columnsMode)
            {
                case COLUMNS_WEEKS:
                    columnsText = "Sloupce jsou kalendářní týdny (pondělí až neděle).";
                    break;
                case COLUMNS_DAYS:
                    columnsText = "Sloupce jsou jednotlivé dny.";
                    break;
                case COLUMNS_MONTHS:
                    columnsText = "Sloupce jsou kalendářní měsíce.";
                    break;
                default:
                    columnsText = "Sloupce jsou kalendářní roky.";
                    break;
            }

            string rowsText;
            switch (rowsMode)
            {
                case ROWS_DAYS_OF_WEEK:
                    rowsText = "Řádky rozdělují objednávky daného sloupce podle dne v týdnu.";
                    break;
                case ROWS_HOURS_AND_DAYS:
                    rowsText = "Řádky rozdělují objednávky daného sloupce podle dne v týdnu a hodiny (např. 14:00–14:59 jsou objednávky od 14:00:00 do 14:59:59).";
                    break;
                case ROWS_MONTHS:
                    rowsText = "Řádky rozdělují objednávky daného sloupce podle kalendářního měsíce bez ohledu na rok.";
                    break;
                default:
                    rowsText = "Řádky rozdělují objednávky daného sloupce podle hodiny bez ohledu na den v týdnu (např. 14:00–14:59 jsou objednávky od 14:00:00 do 14:59:59).";
                    break;
            }

            return string.Join("\n", new[]
            {
                metricText,
                $"{columnsText} {rowsText}",
                "Rozhoduje datum a čas nákupu objednávky. Zahrnuty jsou všechny objednávky odpovídající zadaným filtrům.",
                "Sloupce pokrývají období od první do poslední nalezené objednávky, krajní sloupce proto mohou zahrnovat jen část týdne, měsíce nebo roku. Hodnota 0 znamená, že v daném čase nebyla žádná objednávka."
            });
        }

        private class HourBucket
        {
            public DateTime Day { get; set; }
            public int Hour { get; set; }
            public int OrdersCount { get; set; }
            public decimal Revenue { get; set; }
        }

        private class Period
        {
            public DateTime Start { get; set; }
            public string Title { get; set; }
        }
    }
}
