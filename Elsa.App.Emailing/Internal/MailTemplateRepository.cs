using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Elsa.App.Emailing.Model;
using Elsa.Smtp.Core;

namespace Elsa.App.Emailing.Internal
{
    public class MailTemplateRepository : IMailTemplateRepository
    {
        private const string SUBJECT_PREFIX = "<!-- SUBJECT:";
        private const string SUBJECT_SUFFIX = "-->";

        private static readonly Regex _subjectRegex = new Regex(
            @"^\s*<!--\s*SUBJECT:\s*(?<subject>.*?)\s*-->\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Encoding _fileEncoding = new UTF8Encoding(false);

        public List<MailTemplateModel> GetAll()
        {
            EnsureDirectory();
            var templates = Directory.EnumerateFiles(
                    MailTemplatePictureStore.ROOT_DIRECTORY,
                    "*",
                    SearchOption.TopDirectoryOnly)
                .Where(IsTemplateFile)
                .Select(path => ReadTemplate(path, false))
                .ToList();

            var duplicate = templates
                .GroupBy(template => template.TypeName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
            {
                throw new InvalidOperationException(
                    $"E-mailová šablona '{duplicate.Key}' existuje současně jako HTML i prostý text.");
            }

            return templates
                .OrderBy(template => template.TypeName, StringComparer.CurrentCultureIgnoreCase)
                .Select(template => new MailTemplateModel
                {
                    TypeName = template.TypeName,
                    Subject = template.Subject,
                    BodyFormat = template.BodyFormat
                })
                .ToList();
        }

        public MailTemplateModel Get(string typeName)
        {
            return ReadTemplate(FindTemplatePath(typeName), true);
        }

        public MailTemplateModel GetByTypeName(string typeName)
        {
            return Get(typeName);
        }

        public bool Exists(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return false;
            }

            EnsureDirectory();
            return FindTemplatePaths(NormalizeTypeName(typeName)).Any();
        }

        public MailTemplateModel Save(MailTemplateModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var typeName = NormalizeTypeName(model.TypeName);
            var bodyFormat = NormalizeBodyFormat(model.BodyFormat);
            var originalTypeName = string.IsNullOrWhiteSpace(model.OriginalTypeName)
                ? null
                : NormalizeTypeName(model.OriginalTypeName);
            var originalBodyFormat = originalTypeName == null
                ? null
                : NormalizeBodyFormat(model.OriginalBodyFormat);

            ValidateSubject(model.Subject);
            EnsureDirectory();

            var targetPath = GetTemplatePath(typeName, bodyFormat);
            var sourcePath = originalTypeName == null
                ? null
                : GetTemplatePath(originalTypeName, originalBodyFormat);

            if (sourcePath != null && !File.Exists(sourcePath))
            {
                throw new InvalidOperationException($"Původní e-mailová šablona '{originalTypeName}' neexistuje.");
            }

            var conflictingTemplateExists = FindTemplatePaths(typeName).Any(path =>
                !string.Equals(path, sourcePath, StringComparison.OrdinalIgnoreCase));
            if (conflictingTemplateExists)
            {
                throw new InvalidOperationException($"E-mailová šablona '{typeName}' již existuje.");
            }

            var temporaryPath = Path.Combine(
                MailTemplatePictureStore.ROOT_DIRECTORY,
                $".{Guid.NewGuid():N}.mailtemplate.tmp");

            try
            {
                File.WriteAllText(
                    temporaryPath,
                    $"{SUBJECT_PREFIX} {model.Subject ?? string.Empty} {SUBJECT_SUFFIX}\r\n{model.Body ?? string.Empty}",
                    _fileEncoding);

                if (sourcePath != null
                    && !string.Equals(sourcePath, targetPath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Move(temporaryPath, targetPath);
                    try
                    {
                        File.Delete(sourcePath);
                    }
                    catch
                    {
                        File.Delete(targetPath);
                        throw;
                    }
                }
                else if (File.Exists(targetPath))
                {
                    File.Replace(temporaryPath, targetPath, null);
                }
                else
                {
                    File.Move(temporaryPath, targetPath);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }

            return ReadTemplate(targetPath, true);
        }

        public void Delete(string typeName, string bodyFormat)
        {
            var templatePath = GetTemplatePath(NormalizeTypeName(typeName), NormalizeBodyFormat(bodyFormat));
            if (!File.Exists(templatePath))
            {
                throw new InvalidOperationException($"E-mailová šablona '{typeName}' neexistuje.");
            }

            var deletedPath = $"{templatePath}.deleted_{DateTime.Now:yyyyMMdd_HHmmssfff}";
            File.Move(templatePath, deletedPath);
        }

        private static MailTemplateModel ReadTemplate(string path, bool includeBody)
        {
            using (var reader = new StreamReader(path, _fileEncoding, true))
            {
                var subjectLine = reader.ReadLine();
                var subjectMatch = _subjectRegex.Match(subjectLine ?? string.Empty);
                if (!subjectMatch.Success)
                {
                    throw new InvalidOperationException(
                        $"První řádek šablony '{Path.GetFileName(path)}' musí mít formát {SUBJECT_PREFIX} ... {SUBJECT_SUFFIX}.");
                }

                var bodyFormat = GetBodyFormat(path);
                var typeName = Path.GetFileNameWithoutExtension(path);
                return new MailTemplateModel
                {
                    TypeName = typeName,
                    OriginalTypeName = typeName,
                    Subject = subjectMatch.Groups["subject"].Value,
                    Body = includeBody ? reader.ReadToEnd() : null,
                    BodyFormat = bodyFormat,
                    OriginalBodyFormat = bodyFormat
                };
            }
        }

        private static string FindTemplatePath(string typeName)
        {
            var normalizedTypeName = NormalizeTypeName(typeName);
            EnsureDirectory();
            var paths = FindTemplatePaths(normalizedTypeName).ToList();
            if (paths.Count == 0)
            {
                throw new InvalidOperationException($"E-mailová šablona '{normalizedTypeName}' neexistuje.");
            }

            if (paths.Count > 1)
            {
                throw new InvalidOperationException(
                    $"E-mailová šablona '{normalizedTypeName}' existuje současně jako HTML i prostý text.");
            }

            return paths[0];
        }

        private static IEnumerable<string> FindTemplatePaths(string typeName)
        {
            var plainTextPath = GetTemplatePath(typeName, MailTemplateBodyFormats.PlainText);
            var htmlPath = GetTemplatePath(typeName, MailTemplateBodyFormats.Html);

            if (File.Exists(plainTextPath))
            {
                yield return plainTextPath;
            }

            if (File.Exists(htmlPath))
            {
                yield return htmlPath;
            }
        }

        private static string GetTemplatePath(string typeName, string bodyFormat)
        {
            var extension = bodyFormat == MailTemplateBodyFormats.Html ? ".html" : ".txt";
            return Path.Combine(MailTemplatePictureStore.ROOT_DIRECTORY, typeName + extension);
        }

        private static string NormalizeTypeName(string typeName)
        {
            var normalizedTypeName = typeName?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedTypeName))
            {
                throw new InvalidOperationException("Název šablony musí být vyplněný.");
            }

            if (normalizedTypeName == "."
                || normalizedTypeName == ".."
                || normalizedTypeName.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                || normalizedTypeName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                || normalizedTypeName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new InvalidOperationException(
                    "Název šablony musí být platný název souboru bez přípony .html nebo .txt.");
            }

            return normalizedTypeName;
        }

        private static string NormalizeBodyFormat(string bodyFormat)
        {
            var normalizedBodyFormat = bodyFormat ?? MailTemplateBodyFormats.PlainText;
            if (normalizedBodyFormat != MailTemplateBodyFormats.PlainText
                && normalizedBodyFormat != MailTemplateBodyFormats.Html)
            {
                throw new InvalidOperationException("Vybraný formát těla e-mailu není podporovaný.");
            }

            return normalizedBodyFormat;
        }

        private static void ValidateSubject(string subject)
        {
            if ((subject ?? string.Empty).IndexOfAny(new[] { '\r', '\n' }) >= 0
                || (subject ?? string.Empty).Contains(SUBJECT_SUFFIX))
            {
                throw new InvalidOperationException("Předmět musí být na jednom řádku a nesmí obsahovat '-->'.");
            }
        }

        private static bool IsTemplateFile(string path)
        {
            var extension = Path.GetExtension(path);
            return extension.Equals(".html", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".txt", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetBodyFormat(string path)
        {
            return Path.GetExtension(path).Equals(".html", StringComparison.OrdinalIgnoreCase)
                ? MailTemplateBodyFormats.Html
                : MailTemplateBodyFormats.PlainText;
        }

        private static void EnsureDirectory()
        {
            Directory.CreateDirectory(MailTemplatePictureStore.ROOT_DIRECTORY);
        }
    }
}
