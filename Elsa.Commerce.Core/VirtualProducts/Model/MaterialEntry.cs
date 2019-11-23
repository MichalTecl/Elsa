using System;
using System.Linq;
using System.Text.RegularExpressions;
using Elsa.Common;
using Elsa.Common.Utils;

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
            return $"{StringUtil.FormatDecimal(Amount)} {UnitName} {MaterialName}";
        }

        public Amount GetAmount(IUnitRepository ur)
        {
            var unit = ur.GetUnitBySymbol(UnitName).Ensure($"Neznámá měrná jednotka \"{UnitName}\"");
            return new Amount(Amount, unit);
        }

        public static MaterialEntry Parse(string entry, bool allowMissingMaterialName = false)
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

            int expectedNumberOfMatches = allowMissingMaterialName ? 3 : 4;

            if (grps.Length != expectedNumberOfMatches)
            {
                throw new ArgumentException($"Chybný formát \"{entry}\"");
            }

            decimal amount;
            if (!decimal.TryParse(grps[1], out amount))
            {
                throw new ArgumentException("Chybný formát množství");
            }

            var result = new MaterialEntry() { Amount = amount, UnitName = grps[2] };
            
            if(grps.Length > 3)
            {
                result.MaterialName = grps.Last();
            }

            return result;
        }
    }

}
