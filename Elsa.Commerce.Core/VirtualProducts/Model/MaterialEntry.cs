using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.VirtualProducts.Model
{
    public class MaterialEntry
    {
        private static readonly Regex s_entryRegex = new Regex(@"((\d+(?:[\.\,]\d{1,1000})?))(\s*)(\S*)(\s*)(.*)");

        public decimal Amount { get; set; }

        public string UnitName { get; set; }

        public string MaterialName { get; set; }

        public override string ToString()
        {
            return $"{Amount}{UnitName} {MaterialName}";
        }

        public static MaterialEntry Parse(string entry)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                throw new ArgumentNullException(nameof(entry));
            }

            entry = entry.Trim();

            var match = s_entryRegex.Match(entry);
            if (!match.Success)
            {
                throw new ArgumentException($"Chybný formát \"{entry}\"");
            }

            var grps =
                match.Groups.OfType<Group>()
                    .SelectMany(g => g.Captures.OfType<Capture>().Select(c => c.Value))
                    .Distinct()
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();

            if (grps.Length != 4)
            {
                throw new ArgumentException($"Chybný formát \"{entry}\"");
            }

            decimal amount;
            if (!decimal.TryParse(grps[1], out amount))
            {
                throw new ArgumentException("Chybný formát množství");
            }

            return new MaterialEntry() { Amount = amount, UnitName = grps[2], MaterialName = grps[3] };
        }
    }

}
