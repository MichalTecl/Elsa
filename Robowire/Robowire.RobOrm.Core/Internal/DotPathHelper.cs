using System;

namespace Robowire.RobOrm.Core.Internal
{
    public static class DotPathHelper
    {
        public static string TrimDots(string a)
        {
            if (a == null)
            {
                return string.Empty;
            }

            a = a.Trim();

            while (a.EndsWith("."))
            {
                a = a.Substring(0, a.Length - 1);
            }

            while (a.StartsWith("."))
            {
                a = a.Substring(1);
            }

            return a;
        }

        public static string Combine(string a, string b)
        {
            a = TrimDots(a);
            b = TrimDots(b);

            if (string.IsNullOrWhiteSpace(a))
            {
                return b;
            }

            if (string.IsNullOrWhiteSpace(b))
            {
                return a;
            }

            return $"{a}.{b}";
        }

        public static string GetParent(string path)
        {
            path = TrimDots(path);

            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            var lastDot = path.LastIndexOf(".");

            if (lastDot < 0)
            {
                return null;
            }

            return path.Substring(0, lastDot);
        }
        
        public static Tuple<string, string> SplitToEntityAndAttribute(string path)
        {
            var parent = GetParent(path);
            var last = path.Substring(parent.Length);
            
            return new Tuple<string, string>(TrimDots(parent), TrimDots(last));
        }
    }

    
}
