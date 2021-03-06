
INSERT INTO ScheduledJob (ProjectId, Name, SecondsInterval, ModuleClass, CustomData, SequencePriority, ActiveFrom)
SELECT 1, 'Db Backup', 10, 'Elsa.Jobs.DbBackup.DbBackupJob, Elsa.Jobs.DbBackup', 'null', 1, GETDATE()
  WHERE NOT EXISTS(SELECT TOP 1 1 FROM ScheduledJob WHERE Name = 'Db Backup');

INSERT INTO JobSchedule (ProjectId, ScheduledJobId, Active, CanBeStartedManually, RetryMinutes, LoopLaunchPriority, Uid)
SELECT ProjectId, Id, 1, 1, 1, 1, 'DB_BACKUP'
  FROM ScheduledJob
 WHERE Name = 'Db Backup'
   AND NOT EXISTS(SELECT TOP 1 1 FROM JobSchedule WHERE Uid =  'DB_BACKUP');


INSERT INTO SysConfig (ProjectId, [Key], ValueJson, ValidFrom, InsertUserId)
SELECT 1, src.K, src.V, GETDATE(), 2
	FROM (
	SELECT 'DbBackup.FtpUrl' K, '"1476.disk.zonercloud.net"' V UNION
	SELECT 'DbBackup.FtpUser' K, '"1476"' V UNION
	SELECT 'DbBackup.FtpPassword' K, '""' V UNION
	SELECT 'DbBackup.FtpPassword' K, '""' V UNION
	SELECT 'DbBackup.RemoteFolder' K, '"DBBAK"' V) src
WHERE NOT EXISTS(SELECT TOP 1 1 FROM SysConfig e WHERE e.[Key] = src.K);

   