using System.Collections;

namespace Robowire.RoboApi
{
    public static class RequestContextFlags
    {
        private static readonly object s_suppressSessionCookieWriteKey = new object();

        public static void SetSuppressSessionCookieWrite(IDictionary items, bool value)
        {
            if (items == null)
            {
                return;
            }

            items[s_suppressSessionCookieWriteKey] = value;
        }

        public static bool GetSuppressSessionCookieWrite(IDictionary items)
        {
            return items?[s_suppressSessionCookieWriteKey] as bool? == true;
        }
    }
}
