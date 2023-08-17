using Elsa.Core.Entities.Commerce.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Core.Entities.Commerce.Extensions
{
    public static class AddressExtensions
    {
        public static string GetFormattedStreetAndHouseNr(this IPostalAddress address) 
        {
            //  Kopečná 1069/37 - 1069 je číslo popisné, 37 je číslo orientační

            var parts = $"{address.DescriptiveNumber}/{address.OrientationNumber}"
                .Split('/', ' ')
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .Take(2);

            var nr = string.Join("/", parts);

            return $"{address.Street?.Trim()} {nr?.Trim()}";
        }
    }
}
