using System;
using System.Globalization;
using DocGen;

namespace Elsa.Jobs.BulletinGeneration
{
    public sealed class ExampleRevenueChapter : IDocumentChapter
    {
        public DocumentChapter Build(DateTime generatedAt)
        {
            var chart = new GroupedStackedChart { Title = "Měsíční tržby — meziroční srovnání", Unit = "Kč" };
            chart.Segments.Add(new ChartSegment { Label = "Maloobchod", Color = "#38A89D" });
            chart.Segments.Add(new ChartSegment { Label = "Velkoobchod", Color = "#5367C8" });

            var firstMonth = new DateTime(generatedAt.Year, generatedAt.Month, 1).AddMonths(-6);
            var retail = new decimal[] { 180000, 205000, 192000, 230000, 215000, 248000 };
            var wholesale = new decimal[] { 260000, 245000, 310000, 285000, 340000, 325000 };
            var previousRetail = new decimal[] { 155000, 190000, 200000, 185000, 220000, 210000 };
            var previousWholesale = new decimal[] { 230000, 255000, 240000, 270000, 295000, 280000 };

            for (var i = 0; i < 6; i++)
            {
                var month = firstMonth.AddMonths(i);
                var group = new ChartGroup { Label = month.ToString("MMMM", CultureInfo.GetCultureInfo("cs-CZ")) };
                group.Columns.Add(new ChartColumn { Label = month.AddYears(-1).Year.ToString(), Values = new[] { previousRetail[i], previousWholesale[i] } });
                group.Columns.Add(new ChartColumn { Label = month.Year.ToString(), Values = new[] { retail[i], wholesale[i] } });
                chart.Groups.Add(group);
            }

            return new DocumentChapter
            {
                Title = "Jak se vyvíjejí naše tržby",
                Text = "Tato kapitola používá pouze smyšlená ukázková data; nejde o skutečné výsledky podniku.\n"
                    + "Graf zobrazuje šest dokončených měsíců. V každé dvojici je vlevo stejný měsíc předchozího roku "
                    + "a vpravo sledovaný měsíc. Barevná patra rozlišují maloobchod a velkoobchod.",
                Chart = chart
            };
        }
    }
}
