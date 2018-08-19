using System;

using Elsa.Jobs.Common;

using Robowire.RobOrm.Core;

namespace Elsa.Jobs.PrefillCalender
{
    public class PrefillCalendarJob : IExecutableJob
    {
        private IDatabase m_database;

        public PrefillCalendarJob(IDatabase database)
        {
            m_database = database;
        }

        public void Run(string customDataJson)
        {
            var startDt = (m_database.Sql().Execute("SELECT MAX(Day) FROM BaseCalendar").Scalar<DateTime?>() ?? DateTime.Now.AddYears(-1)).Date;

            var requiredLastDt = (DateTime.Now.AddYears(1)).Date;

            while (startDt < requiredLastDt)
            {
                startDt = startDt.AddDays(1).Date;
                Console.WriteLine($"Doplnuji kalendar pro {startDt}");
                var wd = startDt.DayOfWeek != DayOfWeek.Saturday && startDt.DayOfWeek != DayOfWeek.Sunday;
                m_database.Sql().ExecuteWithParams("INSERT INTO BaseCalendar (Day, IsBusinessDay) VALUES ({0}, {1})", startDt, wd).NonQuery();
            }

        }
    }
}
