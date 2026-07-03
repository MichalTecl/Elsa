namespace Elsa.App.PublicFiles
{
    internal static class PublicFilesCacheKeyHelper
    {
        public static string GetCacheKey(string customerName, string fileType)
        {
            return $"PublicFiles/{customerName.GetHashCode()}/{fileType.GetHashCode()}";
        }
    }
}
