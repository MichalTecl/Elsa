using System;

namespace Elsa.Common.Utils
{
    public static class DateUtil
    {
        public static string FormatDateWithAgo(DateTime dt, bool includeTime = false)
        {
            var days = (int)((DateTime.Now - dt).TotalDays);

            var agoWord = string.Empty;

            switch (days)
            {
                case 0:
                    agoWord = "dnes";

                    if (includeTime)
                    {
                        agoWord = GetTimeText(dt);
                    }

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

        private static string GetTimeText(DateTime dt)
        {
            var diff = DateTime.Now - dt;

            if (diff.TotalHours > 1)
            {
                return $"Více než {((int)diff.TotalHours)} hodin";
            }

            if (diff.TotalMinutes < 2)
            {
                return "Právě teď";
            }

            return $"{((int)diff.TotalMinutes)} minut";
        }

        public static void GetMonthDt(int year, int month, out DateTime from, out DateTime to)
        {
            from = new DateTime(year, month, 1).Date;
            to = from.AddMonths(1).Date;
        }
    }
}
