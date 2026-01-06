using Elsa.Core.Entities.Commerce.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Integration.ShipmentProviders.Zasilkovna.ShipmentRequestDocumentGenerators
{
    internal static class ShipmentDataHelper
    {
        public static string GetFormattedHouseNumber(IAddress address)
        {
            if (address == null)
            {
                return null;
            }

            return FormatHouseNumber(address.OrientationNumber, address.DescriptiveNumber);
        }

        public static string FormatHouseNumber(string orientation, string descriptive)
        {
            if (string.IsNullOrWhiteSpace(orientation))
            {
                return descriptive;
            }

            if (string.IsNullOrWhiteSpace(descriptive))
            {
                return orientation;
            }

            if (descriptive.Contains("/"))
                return descriptive;

            if (orientation.Contains("/"))
                return orientation;

            return $"{descriptive}/{orientation}";
        }

        public static Tuple<string, string> SplitPhonePrefixBody(string phone)
        {
            // +420775154809
            var prefix = string.Empty;
            var body = string.Empty;

            if (!string.IsNullOrWhiteSpace(phone))
            {
                phone = phone.Replace(" ", string.Empty).Replace("-", string.Empty).Replace("/", string.Empty);

                body = phone;
                if (body.Length >= 9)
                {
                    body = body.Substring(body.Length - 9);
                    prefix = phone.Substring(0, phone.Length - 9);
                }
            }

            if (prefix.Length > 4 && prefix.StartsWith("00"))
                prefix = $"+{prefix.Substring(2)}";

            return new Tuple<string, string>(prefix, body);
        }
    }
}
