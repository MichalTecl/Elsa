using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace Elsa.Integration.ShipmentProviders.Zasilkovna
{
    internal static class PacketaShipmentListParser
    {
        internal static void AddRows(string html, string sender, Dictionary<string, HashSet<string>> index)
        {
            var document = PacketaWebClient.ParseHtml(html);
            var rows = document.DocumentNode.Descendants("tr").Where(n => n.Attributes["data-id"] != null).ToList();
            if (rows.Count == 0 && !string.IsNullOrWhiteSpace(html)
                && !document.DocumentNode.Descendants().Any(n => HasClass(n, "datagrid-empty")))
                throw new InvalidOperationException("Zásilkovna vrátila neznámý formát prázdné tabulky.");

            foreach (var row in rows)
            {
                var rowSender = Cell(row, "col-senderId");
                if (!string.Equals(rowSender, sender, StringComparison.OrdinalIgnoreCase)) continue;
                var reference = Cell(row, "col-number");
                var id = row.GetAttributeValue("data-id", "");
                if (reference.Length == 0 || id.Length == 0 || id.Any(c => c < '0' || c > '9'))
                    throw new InvalidOperationException("Zásilka Zásilkovny nemá platnou referenci objednávky nebo identifikátor.");

                var trackingNumber = "Z" + id;
                // The displayed barcode cell may also contain a different carrier's barcode.
                // Validate the Packeta ID against its own tracking link instead.
                if (!row.Descendants("a").Any(a => a.GetAttributeValue("href", "") == "https://tracking.packeta.com/cs/?id=" + trackingNumber))
                    throw new InvalidOperationException("Identifikátor zásilky Zásilkovny neodpovídá trackingovému odkazu.");

                if (!index.TryGetValue(reference, out var numbers))
                    index[reference] = numbers = new HashSet<string>(StringComparer.Ordinal);
                numbers.Add(trackingNumber);
            }
        }

        private static string Cell(HtmlNode row, string cssClass)
        {
            var cell = row.Elements("td").SingleOrDefault(n => HasClass(n, cssClass));
            if (cell == null)
                throw new InvalidOperationException($"V tabulce Zásilkovny chybí sloupec {cssClass}.");
            return HtmlEntity.DeEntitize(cell.InnerText).Trim();
        }

        private static bool HasClass(HtmlNode node, string cssClass)
        {
            return node.GetAttributeValue("class", "").Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Contains(cssClass);
        }
    }
}
