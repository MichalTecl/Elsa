
IF NOT EXISTS(SELECT TOP 1 1 FROM ScheduledJob WHERE ModuleClass = 'Elsa.Jobs.FinancialReportsGeneration.FinDataGenerationJob, Elsa.Jobs.FinancialReportsGeneration')
BEGIN
	INSERT INTO ScheduledJob (ProjectId, Name, SecondsInterval, ModuleClass, CustomData, SequencePriority, ActiveFrom)
	VALUES (1, 'Generování účetních dat', 1, 'Elsa.Jobs.FinancialReportsGeneration.FinDataGenerationJob, Elsa.Jobs.FinancialReportsGeneration', 'null', 1, GETDATE());

	INSERT INTO JobSchedule (ProjectId, ScheduledJobId, Active, CanBeStartedManually, Uid)
	SELECT TOP 1 ProjectId, Id, 1, 1, 'GENEROVANI_UCT_REPORTU'
	  FROM ScheduledJob
	 WHERE Id = SCOPE_IDENTITY();
END

IF NOT EXISTS(SELECT TOP 1 1 FROM EmailRecipientList WHERE GroupName = 'Ucetni Vystupy')
BEGIN
	INSERT INTO EmailRecipientList (ProjectId, GroupName, Addresses)
	SELECT Id, 'Ucetni Vystupy', 'mtecl.prg@gmail.com'
	  FROM Project;	  
END