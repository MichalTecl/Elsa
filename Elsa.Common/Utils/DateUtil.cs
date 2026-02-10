using System;

namespace Elsa.Common.Utils
{
    public static class DateUtil
    {
        public static string FormatDateWithAgo(DateTime dt, bool includeTime = false)
        {
            var days = (DateTime.Today - dt.Date).Days;

            string agoWord;

            switch (days)
            {
                case 0:
                    agoWord = includeTime ? GetTimeText(dt) : "dnes";
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

        public static int GetRemainingMonths(DateTime eventDate, DateTime? now = null) 
        {
            DateTime current = now ?? DateTime.Now;

            int months =
                (eventDate.Year - current.Year) * 12 +
                (eventDate.Month - current.Month);

            // pokud ještě nenastal den v cílovém měsíci, měsíc není celý
            if (eventDate.Day < current.Day)
            {
                months--;
            }

            return months;
        }
    }
}
