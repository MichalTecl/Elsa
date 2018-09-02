using System;

using Elsa.Common.Logging;
using Elsa.Jobs.Common;

using Robowire.RobOrm.Core;

namespace Elsa.Jobs.PrefillCalender
{
    public class PrefillCalendarJob : IExecutableJob
    {
        private readonly IDatabase m_database;
        private readonly ILog m_log;

        public PrefillCalendarJob(IDatabase database, ILog log)
        {
            m_database = database;
            m_log = log;
        }

        public void Run(string customDataJson)
        {
            m_log.Info("Job \"Udrzba zakladniho kalendare\" spusten");

            var startDt = (m_database.Sql().Execute("SELECT MAX(Day) FROM BaseCalendar").Scalar<DateTime?>() ?? DateTime.Now.AddYears(-1)).Date;

            var requiredLastDt = (DateTime.Now.AddYears(1)).Date;

            while (startDt < requiredLastDt)
            {
                startDt = startDt.AddDays(1).Date;
                m_log.Info($"Doplnuji kalendar pro {startDt}");
                var wd = startDt.DayOfWeek != DayOfWeek.Saturday && startDt.DayOfWeek != DayOfWeek.Sunday;
                m_database.Sql().ExecuteWithParams("INSERT INTO BaseCalendar (Day, IsBusinessDay) VALUES ({0}, {1})", startDt, wd).NonQuery();
            }

        }
    }
}
