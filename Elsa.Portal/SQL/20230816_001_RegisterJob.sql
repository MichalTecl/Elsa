IF NOT EXISTS(SELECT TOP 1 1 FROM ScheduledJob WHERE ModuleClass = 'Elsa.Jobs.ExternalSystemsDataPush.DataPushJob, Elsa.Jobs.ExternalSystemsDataPush')
BEGIN
	INSERT INTO ScheduledJob (ProjectId, Name, SecondsInterval, ModuleClass, CustomData, SequencePriority, ActiveFrom)
	VALUES (1, 'Data Push', 1, 'Elsa.Jobs.ExternalSystemsDataPush.DataPushJob, Elsa.Jobs.ExternalSystemsDataPush', 'null', 1, GETDATE());

	INSERT INTO JobSchedule (ProjectId, ScheduledJobId, Active, CanBeStartedManually, Uid)
	SELECT TOP 1 ProjectId, Id, 1, 1, 'DATA_PUSH'
	  FROM ScheduledJob
	 WHERE Id = SCOPE_IDENTITY();
END