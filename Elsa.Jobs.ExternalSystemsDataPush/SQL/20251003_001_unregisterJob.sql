-- ================================
-- Delete "Data Push" job & schedule created by the given script
-- ================================

SET NOCOUNT ON;

DECLARE 
    @ProjectId   INT            = 1,
    @Name        NVARCHAR(200)  = N'Data Push',
    @ModuleClass NVARCHAR(400)  = N'Elsa.Jobs.ExternalSystemsDataPush.DataPushJob, Elsa.Jobs.ExternalSystemsDataPush',
    @Uid         NVARCHAR(100)  = N'DATA_PUSH';

-- 1) Preview: what will be deleted
PRINT 'Preview of rows slated for deletion:';

PRINT '-> ScheduledJob candidates:';
SELECT sj.*
FROM dbo.ScheduledJob sj
WHERE sj.ProjectId = @ProjectId
  AND sj.Name = @Name
  AND sj.ModuleClass = @ModuleClass;

PRINT '-> JobSchedule candidates:';
SELECT js.*
FROM dbo.JobSchedule js
WHERE js.Uid = @Uid
  AND js.ScheduledJobId IN (
        SELECT sj.Id
        FROM dbo.ScheduledJob sj
        WHERE sj.ProjectId = @ProjectId
          AND sj.Name = @Name
          AND sj.ModuleClass = @ModuleClass
  );

-- =========================================
-- 2) Delete inside a transaction
--    (Uncomment ROLLBACK to dry-run)
-- =========================================
BEGIN TRY
    BEGIN TRAN;

    -- Delete children first
    DELETE js
    FROM dbo.JobSchedule js
    WHERE js.Uid = @Uid
      AND js.ScheduledJobId IN (
            SELECT sj.Id
            FROM dbo.ScheduledJob sj
            WHERE sj.ProjectId = @ProjectId
              AND sj.Name = @Name
              AND sj.ModuleClass = @ModuleClass
      );

    PRINT CONCAT('Deleted JobSchedule rows: ', @@ROWCOUNT);

    -- Delete the parent job(s)
    DELETE sj
    FROM dbo.ScheduledJob sj
    WHERE sj.ProjectId = @ProjectId
      AND sj.Name = @Name
      AND sj.ModuleClass = @ModuleClass;

    PRINT CONCAT('Deleted ScheduledJob rows: ', @@ROWCOUNT);

    COMMIT TRAN;
    PRINT 'Deletion committed.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRAN;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE(),
            @ErrNum INT = ERROR_NUMBER(),
            @ErrSev INT = ERROR_SEVERITY(),
            @ErrSta INT = ERROR_STATE(),
            @ErrLin INT = ERROR_LINE(),
            @ErrProc NVARCHAR(200) = ISNULL(ERROR_PROCEDURE(), N'');
    RAISERROR('Delete failed (%d, Sev %d, State %d) at line %d in %s: %s',
              @ErrSev, 1, @ErrNum, @ErrSev, @ErrSta, @ErrLin, @ErrProc, @ErrMsg);
END CATCH;
