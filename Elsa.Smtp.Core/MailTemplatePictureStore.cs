using System;
using System.Collections.Generic;
using System.IO;

namespace Elsa.Smtp.Core
{
    public static class MailTemplatePictureStore
    {
        public const string ROOT_DIRECTORY = @"C:\Elsa\StaticPictures";

        private static readonly IReadOnlyDictionary<string, string> _contentTypes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [".gif"] = "image/gif",
                [".jpeg"] = "image/jpeg",
                [".jpg"] = "image/jpeg",
                [".png"] = "image/png"
            };

        public static string ResolvePath(string pictureReference)
        {
            if (string.IsNullOrWhiteSpace(pictureReference))
            {
                throw new InvalidOperationException("Reference obrázku nesmí být prázdná.");
            }

            var trimmedReference = pictureReference.Trim();
            if (Uri.TryCreate(trimmedReference, UriKind.Absolute, out _)
                || Path.IsPathRooted(trimmedReference)
                || trimmedReference.IndexOfAny(new[] { '?', '#' }) >= 0)
            {
                throw new InvalidOperationException(
                    $"Obrázek '{pictureReference}' musí být zadán relativní cestou do {ROOT_DIRECTORY}.");
            }

            string decodedReference;
            try
            {
                decodedReference = Uri.UnescapeDataString(trimmedReference).Replace('/', Path.DirectorySeparatorChar);
            }
            catch (UriFormatException)
            {
                throw new InvalidOperationException($"Reference obrázku '{pictureReference}' není platná.");
            }

            if (Uri.TryCreate(decodedReference, UriKind.Absolute, out _)
                || Path.IsPathRooted(decodedReference)
                || decodedReference.IndexOfAny(new[] { ':', '?', '#' }) >= 0)
            {
                throw new InvalidOperationException(
                    $"Obrázek '{pictureReference}' musí být zadán relativní cestou do {ROOT_DIRECTORY}.");
            }

            var rootPath = Path.GetFullPath(ROOT_DIRECTORY)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var resolvedPath = Path.GetFullPath(Path.Combine(rootPath, decodedReference));
            if (!resolvedPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Obrázek '{pictureReference}' leží mimo povolený adresář {ROOT_DIRECTORY}.");
            }

            if (!_contentTypes.ContainsKey(Path.GetExtension(resolvedPath)))
            {
                throw new InvalidOperationException(
                    $"Obrázek '{pictureReference}' nemá podporovaný formát. Povolené jsou PNG, JPG a GIF.");
            }

            if (!File.Exists(resolvedPath))
            {
                throw new FileNotFoundException(
                    $"Obrázek '{pictureReference}' nebyl v adresáři {ROOT_DIRECTORY} nalezen.",
                    resolvedPath);
            }

            return resolvedPath;
        }

        public static string GetContentType(string picturePath)
        {
            if (!_contentTypes.TryGetValue(Path.GetExtension(picturePath), out var contentType))
            {
                throw new InvalidOperationException($"Soubor '{picturePath}' nemá podporovaný formát obrázku.");
            }

            return contentType;
        }
    }
}
