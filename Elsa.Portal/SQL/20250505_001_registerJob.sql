IF NOT EXISTS(SELECT TOP 1 1 FROM ScheduledJob WHERE ModuleClass = 'Elsa.Jobs.CrmRobots.RunCrmRobotsJob, Elsa.Jobs.CrmRobots')
BEGIN
	INSERT INTO ScheduledJob (ProjectId, Name, SecondsInterval, ModuleClass, CustomData, SequencePriority, ActiveFrom)
	VALUES (1, 'CRM Roboti', 1, 'Elsa.Jobs.CrmRobots.RunCrmRobotsJob, Elsa.Jobs.CrmRobots', 'null', 1, GETDATE());

	INSERT INTO JobSchedule (ProjectId, ScheduledJobId, Active, CanBeStartedManually, Uid)
	SELECT TOP 1 ProjectId, Id, 1, 1, 'CRM_ROBOTS'
	  FROM ScheduledJob
	 WHERE Id = SCOPE_IDENTITY();
END