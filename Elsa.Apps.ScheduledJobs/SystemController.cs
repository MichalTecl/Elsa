using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Robowire.RoboApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.ScheduledJobs
{
    [Controller("system")]
    public class SystemController : ElsaControllerBase
    {
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool InitiateSystemShutdownEx(
            string lpMachineName,
            string lpMessage,
            uint dwTimeout,
            bool bForceAppsClosed,
            bool bRebootAfterShutdown,
            uint dwReason);


        public SystemController(IWebSession webSession, ILog log) : base(webSession, log)
        {       
        }

        public string Reboot()
        {
            WebSession.EnsureUserRight(ScheduledJobsUserRights.RebootSystem);

            if (InitiateSystemShutdownEx(null, "Systém bude restartován za 30 sekund", 30, true, true, 0))
            {
                return "InitiateSystemShutdownEx returned true. Reboot scheduled to 30 seconds.";
            }

            int errorCode = Marshal.GetLastWin32Error();
            return "Chyba: " + errorCode;
        }
    }
}
