IF NOT EXISTS(SELECT TOP 1 1 FROM ScheduledJob WHERE ModuleClass = 'Elsa.Jobs.BuildStoresMap.BuildMapJob, Elsa.Jobs.BuildStoresMap')
BEGIN
	INSERT INTO ScheduledJob (ProjectId, Name, SecondsInterval, ModuleClass, CustomData, SequencePriority, ActiveFrom)
	VALUES (1, 'Mapování Prodejen', 1, 'Elsa.Jobs.BuildStoresMap.BuildMapJob, Elsa.Jobs.BuildStoresMap', 'null', 1, GETDATE());

	INSERT INTO JobSchedule (ProjectId, ScheduledJobId, Active, CanBeStartedManually, Uid)
	SELECT TOP 1 ProjectId, Id, 1, 1, 'STORE_MAP'
	  FROM ScheduledJob
	 WHERE Id = SCOPE_IDENTITY();
END