using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.Utils
{
    public static class DateUtil
    {
        public static string FormatDateWithAgo(DateTime dt)
        {
            var days = (int)((DateTime.Now - dt).TotalDays);

            var agoWord = string.Empty;

            switch (days)
            {
                case 0:
                    agoWord = "dnes";
                    break;
                case 1:
                    agoWord = "včera";
                    break;
                case 2:
                case 3:
                case 4:
                    agoWord = $"{days} dny";
                    break;
                default:
                    agoWord = $"{days} dnů";
                    break;
            }

            return $"{dt:dd.MM.} ({agoWord})";
        }
    }
}
