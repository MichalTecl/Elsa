using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace DocGen
{
    public abstract class DocumentGeneratorBase
    {
        protected abstract object CreateInstance(Type t);

        protected abstract Type[] Chapters { get; }

        protected abstract string Title { get; }

        public string Generate(DateTime generatedAt)
        {
            var chapters = Chapters.Select(type =>
            {
                if (type == null || !typeof(IDocumentChapter).IsAssignableFrom(type))
                    throw new InvalidOperationException("Chapter must implement IDocumentChapter.");

                var chapter = CreateInstance(type) as IDocumentChapter;
                if (chapter == null)
                    throw new InvalidOperationException($"Cannot create chapter {type.FullName}.");

                return chapter.Build(generatedAt)
                    ?? throw new InvalidOperationException($"Chapter {type.FullName} returned no content.");
            }).ToList();

            return new HtmlDocumentRenderer().Render(Title, generatedAt, chapters);
        }

        public string Save(string directory, string filePrefix, DateTime generatedAt)
        {
            if (string.IsNullOrWhiteSpace(filePrefix) || filePrefix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("A valid file prefix is required.", nameof(filePrefix));

            var html = Generate(generatedAt);
            Directory.CreateDirectory(directory);
            var timestamp = generatedAt.ToString("yyyyMMdd_HHmmss_fffffff", CultureInfo.InvariantCulture);
            var path = Path.Combine(directory, filePrefix + "_" + timestamp + ".html");
            var temporaryPath = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                // Publish only a complete document and never overwrite an existing edition.
                File.WriteAllText(temporaryPath, html, new UTF8Encoding(false));
                File.Move(temporaryPath, path);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }

            return path;
        }
    }
}
