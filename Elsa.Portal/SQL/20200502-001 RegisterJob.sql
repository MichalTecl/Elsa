
IF NOT EXISTS(SELECT TOP 1 1 FROM ScheduledJob WHERE ModuleClass = 'Elsa.Jobs.AutomaticQueries.RunAutoqueriesJob, Elsa.Jobs.AutomaticQueries')
BEGIN
	INSERT INTO ScheduledJob (ProjectId, Name, SecondsInterval, ModuleClass, CustomData, SequencePriority, ActiveFrom)
	VALUES (1, 'Automatické dotazy', 1, 'Elsa.Jobs.AutomaticQueries.RunAutoqueriesJob, Elsa.Jobs.AutomaticQueries', 'null', 1, GETDATE());

	INSERT INTO JobSchedule (ProjectId, ScheduledJobId, Active, CanBeStartedManually, Uid)
	SELECT TOP 1 ProjectId, Id, 1, 1, 'AUTOMATICKE_DOTAZY'
	  FROM ScheduledJob
	 WHERE Id = SCOPE_IDENTITY();
END

IF NOT EXISTS(SELECT TOP 1 1 FROM EmailRecipientList WHERE GroupName = 'Autom. dotazy')
BEGIN
	INSERT INTO EmailRecipientList (ProjectId, GroupName, Addresses)
	SELECT Id, 'Autom. dotazy', 'mtecl.prg@gmail.com'
	  FROM Project;	  
END