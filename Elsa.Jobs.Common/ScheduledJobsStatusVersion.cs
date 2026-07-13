using System;

using Elsa.Common.Utils;

namespace Elsa.Jobs.Common
{
    public static class ScheduledJobsStatusVersion
    {
        private const string SHARED_KEY_PREFIX = "ScheduledJobsStatus_";

        public static string Get(int projectId)
        {
            return SharedFilesUtil.GetSharedValue(GetSharedKey(projectId), string.Empty);
        }

        public static void Touch(int projectId)
        {
            SharedFilesUtil.SetSharedValue(GetSharedKey(projectId), Guid.NewGuid().ToString("N"));
        }

        private static string GetSharedKey(int projectId)
        {
            return $"{SHARED_KEY_PREFIX}{projectId}";
        }
    }
}
