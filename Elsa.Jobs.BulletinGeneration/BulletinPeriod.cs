using System;
using System.Globalization;
using DocGen;

namespace Elsa.Jobs.BulletinGeneration
{
    public sealed class BulletinPeriod
    {
        public BulletinPeriod(DateTime generatedAt)
        {
            End = new DateTime(generatedAt.Year, generatedAt.Month, 1);
            Start = End.AddMonths(-3);
        }
        public DateTime Start { get; }
        public DateTime End { get; }
        public DateTime PreviousStart => Start.AddYears(-1);
        public DateTime PreviousEnd => End.AddYears(-1);
        public bool Contains(DateTime date) => date >= Start && date < End;
        public bool ContainsPrevious(DateTime date) => date >= PreviousStart && date < PreviousEnd;
        public string Label => Start.ToString("d. M. yyyy") + " – " + End.AddDays(-1).ToString("d. M. yyyy");
        public string PreviousLabel => PreviousStart.ToString("d. M. yyyy") + " – " + PreviousEnd.AddDays(-1).ToString("d. M. yyyy");
    }

    internal static class BulletinFormat
    {
        private static readonly CultureInfo _culture = CultureInfo.GetCultureInfo("cs-CZ");
        internal static string Number(decimal value) => value.ToString("N0", _culture);
        internal static string Quantity(decimal value) => value.ToString("0.##", _culture);
        internal static string Money(decimal value) => Number(value) + " Kč";
        internal static string Signed(decimal value) => value.ToString("+0;-0;0", _culture);
        internal static string Change(decimal current, decimal previous) => previous == 0 ? "—"
            : Signed(Math.Round((current - previous) / Math.Abs(previous) * 100, 0, MidpointRounding.AwayFromZero)) + "%";
        internal static DocumentTable Table(string title, string note, params string[] columns)
        {
            var table = new DocumentTable { Title = title, Note = note };
            table.Columns.AddRange(columns);
            return table;
        }
        internal static DocumentTableRow Row(params string[] cells)
        {
            var row = new DocumentTableRow();
            foreach (var text in cells)
                row.Cells.Add(new DocumentTableCell { Text = text, IsPositive = text != null && text.StartsWith("+", StringComparison.Ordinal) });
            return row;
        }
    }
}
