using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

using MimeKit;
using MimeKit.Utils;

using QRCoder;

namespace Elsa.Smtp.Core
{
    public static class MailTemplateQrCode
    {
        public const int DEFAULT_DISPLAY_SIZE = 220;

        private const int MIN_DISPLAY_SIZE = 40;
        private const int MAX_DISPLAY_SIZE = 1000;
        private const int PIXELS_PER_MODULE = 8;

        private static readonly Regex _tagRegex = new Regex(
            @"<x:qrcode\b(?<attributes>[^>]*)/\s*>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static readonly Regex _tagStartRegex = new Regex(
            @"<x:qrcode\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _attributeRegex = new Regex(
            @"(?<name>[\w:-]+)\s*=\s*(?<quote>['""])(?<value>.*?)\k<quote>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        public static byte[] GeneratePng(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("Hodnota QR kódu nesmí být prázdná.");
            }

            return PngByteQRCodeHelper.GetQRCode(
                value,
                QRCodeGenerator.ECCLevel.M,
                PIXELS_PER_MODULE);
        }

        public static string Embed(BodyBuilder builder, string html)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var htmlContent = html ?? string.Empty;
            if (_tagStartRegex.Matches(htmlContent).Count != _tagRegex.Matches(htmlContent).Count)
            {
                throw new InvalidOperationException(
                    "Element QR kódu musí mít tvar <x:QRCODE value=\"...\" /> a atributy musí být v uvozovkách.");
            }

            var contentIds = new Dictionary<string, string>(StringComparer.Ordinal);
            return _tagRegex.Replace(htmlContent, match =>
            {
                var attributes = ReadAttributes(match.Groups["attributes"].Value);
                if (!attributes.TryGetValue("value", out var encodedValue))
                {
                    throw new InvalidOperationException("Element x:QRCODE musí obsahovat atribut value.");
                }

                var value = WebUtility.HtmlDecode(encodedValue);
                var displaySize = GetDisplaySize(attributes);

                if (!contentIds.TryGetValue(value, out var contentId))
                {
                    var fileName = $"qrcode-{Guid.NewGuid():N}.png";
                    var linkedQrCode = builder.LinkedResources.Add(fileName, GeneratePng(value));
                    linkedQrCode.ContentId = MimeUtils.GenerateMessageId();
                    contentId = linkedQrCode.ContentId;
                    contentIds[value] = contentId;
                }

                return $"<img src=\"cid:{contentId}\" width=\"{displaySize}\" height=\"{displaySize}\" alt=\"QR kód\" style=\"display:block;border:0;width:{displaySize}px;height:{displaySize}px;\">";
            });
        }

        private static IReadOnlyDictionary<string, string> ReadAttributes(string source)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in _attributeRegex.Matches(source ?? string.Empty))
            {
                result[match.Groups["name"].Value] = match.Groups["value"].Value;
            }

            return result;
        }

        private static int GetDisplaySize(IReadOnlyDictionary<string, string> attributes)
        {
            if (!attributes.TryGetValue("size", out var sizeText))
            {
                return DEFAULT_DISPLAY_SIZE;
            }

            if (!int.TryParse(sizeText, NumberStyles.None, CultureInfo.InvariantCulture, out var size)
                || size < MIN_DISPLAY_SIZE
                || size > MAX_DISPLAY_SIZE)
            {
                throw new InvalidOperationException(
                    $"Atribut size elementu x:QRCODE musí být celé číslo od {MIN_DISPLAY_SIZE} do {MAX_DISPLAY_SIZE}.");
            }

            return size;
        }
    }
}
