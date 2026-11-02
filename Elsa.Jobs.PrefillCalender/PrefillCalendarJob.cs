using System;

using Elsa.Common.Logging;
using Elsa.Jobs.Common;

using RobOrm.Core;

namespace Elsa.Jobs.PrefillCalender
{
    public class PrefillCalendarJob : IExecutableJob
    {
        private readonly IDatabase _database;
        private readonly ILog _log;

        public PrefillCalendarJob(IDatabase database, ILog log)
        {
            _database = database;
            _log = log;
        }

        public void Run(string customDataJson)
        {
            _log.Info("Job \"Udrzba zakladniho kalendare\" spusten");

            var startDt = (_database.Sql().Execute("SELECT MAX(Day) FROM BaseCalendar").Scalar<DateTime?>() ?? DateTime.Now.AddYears(-1)).Date;

            var requiredLastDt = (DateTime.Now.AddYears(1)).Date;

            while (startDt < requiredLastDt)
            {
                startDt = startDt.AddDays(1).Date;
                _log.Info($"Doplnuji kalendar pro {startDt}");
                var wd = startDt.DayOfWeek != DayOfWeek.Saturday && startDt.DayOfWeek != DayOfWeek.Sunday;
                _database.Sql().ExecuteWithParams("INSERT INTO BaseCalendar (Day, IsBusinessDay) VALUES ({0}, {1})", startDt, wd).NonQuery();
            }

        }
    }
}
