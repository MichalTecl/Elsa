IF NOT EXISTS(SELECT TOP 1 1 FROM ScheduledJob WHERE ModuleClass = 'Elsa.Jobs.CrmMailPull.MailPullJob, Elsa.Jobs.CrmMailPull')
BEGIN
	INSERT INTO ScheduledJob (ProjectId, Name, SecondsInterval, ModuleClass, CustomData, SequencePriority, ActiveFrom)
	VALUES (1, N'CRM - Import konverzací', 1, 'Elsa.Jobs.CrmMailPull.MailPullJob, Elsa.Jobs.CrmMailPull', 'null', 1, GETDATE());

	INSERT INTO JobSchedule (ProjectId, ScheduledJobId, Active, CanBeStartedManually, Uid)
	SELECT TOP 1 ProjectId, Id, 1, 1, 'CRM_MAILPULL'
	  FROM ScheduledJob
	 WHERE Id = SCOPE_IDENTITY();
END