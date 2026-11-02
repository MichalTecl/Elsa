using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using Elsa.Common.Logging;
using Elsa.Jobs.Common;
using Ionic.Zip;
using RobOrm.Core;

namespace Elsa.Jobs.DbBackup
{
    public class DbBackupJob : IExecutableJob
    {
        private readonly ITransactionManager<SqlConnection> _database;
        private readonly DbBackupConfig _backupConfig;
        private readonly ILog _log;

        public DbBackupJob(ITransactionManager<SqlConnection> database, DbBackupConfig backupConfig, ILog log)
        {
            _database = database;
            _backupConfig = backupConfig;
            _log = log;
        }

        public void Run(string customDataJson)
        {
            _log.Info("Starting db backup");
            string fileName;

            try
            {
                using (var conn = _database.OpenUnmanagedConnection())
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
                _log.Error("Db backup failed", ex);
                throw;
            }

            _log.Info($"Backup file generated: {fileName}");

            var zipFile = $"{Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName))}_{DateTime.Now.Hour:00}{DateTime.Now.Minute:00}.zip";

            _log.Info($"Starting Zip file generation: {fileName}");

            try
            {
                using (var zip = new ZipFile())
                {
                    //zip.Password = _backupConfig.ZipPassword;
                    zip.AddFile(fileName);
                    zip.Save(zipFile);
                }

                _log.Info($"Zip file created");
            }
            catch (Exception ex)
            {
                _log.Error("Zipping failed", ex);
                throw;
            }

            try
            {
                using (var client = new WebClient())
                {
                    client.Credentials = new NetworkCredential(_backupConfig.FtpUser, _backupConfig.FtpPassword);
                    client.BaseAddress = _backupConfig.FtpUrl;
                    var remfile = $"{_backupConfig.DbBakFtpFolder}/{Path.GetFileName(zipFile)}";

                    _log.Info($"Uploading zip file to {remfile}");

                    client.UploadFile(remfile, WebRequestMethods.Ftp.UploadFile, zipFile);
                }

                _log.Info("Backup uploaded");
            }
            catch (Exception ex)
            {
                _log.Error("Uploading failed", ex);
                throw;
            }

            File.Delete(zipFile);
            _log.Info($"Zip file deleted");

            File.Delete(fileName);
            _log.Info($"Backup file deleted");
        }
    }
}
