using Elsa.Common.Interfaces;
using Elsa.Users.Infrastructure;

namespace Elsa.Apps.ScheduledJobs
{
    [UserRights]
    public static class ScheduledJobsUserRights
    {
        public static readonly UserRight ViewScheduledJobsAdminGrid = new UserRight(nameof(ViewScheduledJobsAdminGrid), "Automatické úlohy - Správce");
    }
}
