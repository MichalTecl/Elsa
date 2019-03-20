using System;
using System.Collections.Concurrent;
using System.IO;

namespace Elsa.EditorBuilder.Content
{
    public static class ContentLoader
    {
        private static readonly ConcurrentDictionary<string, string> s_resources = new ConcurrentDictionary<string, string>();

        public static string EditorTemplate => LoadResource("Elsa.EditorBuilder.Content.EditorTemplate.html");

        public static string LoadResource(string resourceName)
        {
            return s_resources.GetOrAdd(resourceName,
                p =>
                {
                    var assembly = typeof(ContentLoader).Assembly;

                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null)
                        {
                            throw new InvalidOperationException($"Resource {resourceName} does not exist");
                        }

                        using (var reader = new StreamReader(stream))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                });
        }
    }
}
