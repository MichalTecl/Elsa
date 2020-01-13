using Elsa.Common.Configuration;

namespace Elsa.Jobs.DbBackup
{
    [ConfigClass]
    public class DbBackupConfig
    {
        [ConfigEntry("DbBackup.FtpUrl", ConfigEntryScope.Project)]
        public string FtpUrl { get; set; }

        [ConfigEntry("DbBackup.FtpUser", ConfigEntryScope.Project)]
        public string FtpUser { get; set; }

        [ConfigEntry("DbBackup.FtpPassword", ConfigEntryScope.Project)]
        public string FtpPassword { get; set; }

        [ConfigEntry("DbBackup.ZipPassword", ConfigEntryScope.Project)]
        public string ZipPassword { get; set; }

        [ConfigEntry("DbBackup.RemoteFolder", ConfigEntryScope.Project)]
        public string DbBakFtpFolder { get; set; }
    }
}
