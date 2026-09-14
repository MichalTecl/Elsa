IF NOT EXISTS (
    SELECT 1 FROM ScheduledJob
    WHERE ProjectId = 1
      AND ModuleClass = 'Elsa.Jobs.BulletinGeneration.BulletinGenerationJob, Elsa.Jobs.BulletinGeneration'
)
BEGIN
    INSERT INTO ScheduledJob (ProjectId, Name, SecondsInterval, ModuleClass, CustomData, SequencePriority, ActiveFrom)
    VALUES (1, N'Generování Bulletinu', 1, 'Elsa.Jobs.BulletinGeneration.BulletinGenerationJob, Elsa.Jobs.BulletinGeneration', 'null', 1, GETDATE());
END;

IF NOT EXISTS (SELECT 1 FROM JobSchedule WHERE ProjectId = 1 AND Uid = 'BULLETIN_GENERATION')
BEGIN
    INSERT INTO JobSchedule (ProjectId, ScheduledJobId, Active, CanBeStartedManually, Uid)
    SELECT ProjectId, Id, 1, 1, 'BULLETIN_GENERATION'
    FROM ScheduledJob
    WHERE ProjectId = 1
      AND ModuleClass = 'Elsa.Jobs.BulletinGeneration.BulletinGenerationJob, Elsa.Jobs.BulletinGeneration';
END;