using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using Elsa.Common.Logging;
using Elsa.Jobs.Common;
using Ionic.Zip;
using Robowire.RobOrm.Core;

namespace Elsa.Jobs.DbBackup
{
    public class DbBackupJob : IExecutableJob
    {
        private readonly ITransactionManager<SqlConnection> m_database;
        private readonly DbBackupConfig m_backupConfig;
        private readonly ILog m_log;

        public DbBackupJob(ITransactionManager<SqlConnection> database, DbBackupConfig backupConfig, ILog log)
        {
            m_database = database;
            m_backupConfig = backupConfig;
            m_log = log;
        }

        public void Run(string customDataJson)
        {
            m_log.Info("Starting db backup");
            string fileName;

            try
            {
                using (var conn = m_database.OpenUnmanagedConnection())
                {                    
                    conn.Open();

                    using (var cmd = new SqlCommand("sp_backup", conn)
                    {
                        CommandType = CommandType.StoredProcedure,
                        CommandTimeout = 0
                    })
                    {
                        fileName = cmd.ExecuteScalar() as string;

                        if (string.IsNullOrWhiteSpace(fileName))
                        {
                            throw new InvalidOperationException("Backup failed");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_log.Error("Db backup failed", ex);
                throw;
            }

            m_log.Info($"Backup file generated: {fileName}");

            var zipFile = $"{Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName))}_{DateTime.Now.Hour:00}{DateTime.Now.Minute:00}.zip";

            m_log.Info($"Starting Zip file generation: {fileName}");

            try
            {
                using (var zip = new ZipFile())
                {
                    //zip.Password = m_backupConfig.ZipPassword;
                    zip.AddFile(fileName);
                    zip.Save(zipFile);
                }

                m_log.Info($"Zip file created");
            }
            catch (Exception ex)
            {
                m_log.Error("Zipping failed", ex);
                throw;
            }

            try
            {
                using (var client = new WebClient())
                {
                    client.Credentials = new NetworkCredential(m_backupConfig.FtpUser, m_backupConfig.FtpPassword);
                    client.BaseAddress = m_backupConfig.FtpUrl;
                    var remfile = $"{m_backupConfig.DbBakFtpFolder}/{Path.GetFileName(zipFile)}";

                    m_log.Info($"Uploading zip file to {remfile}");

                    client.UploadFile(remfile, WebRequestMethods.Ftp.UploadFile, zipFile);
                }

                m_log.Info("Backup uploaded");
            }
            catch (Exception ex)
            {
                m_log.Error("Uploading failed", ex);
                throw;
            }

            File.Delete(zipFile);
            m_log.Info($"Zip file deleted");

            File.Delete(fileName);
            m_log.Info($"Backup file deleted");
        }
    }
}
