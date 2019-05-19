DECLARE @module NVARCHAR(200) = N'Elsa.GenerateInvoiceForms.GenerateFormsJob, Elsa.GenerateInvoiceForms';

IF NOT EXISTS(SELECT TOP 1 1 FROM ScheduledJob WHERE ModuleClass = @module)
BEGIN
	INSERT INTO ScheduledJob (ProjectId, Name, SecondsInterval, ModuleClass, SequencePriority, ActiveFrom, CustomData)
	VALUES (1, N'Generování účetních dat', 1000, @module, 10, GETDATE(), 'null');

	INSERT INTO JobSchedule 
	([Uid], ProjectId, ScheduledJobId, Active, CanBeStartedManually, RetryMinutes, LoopLaunchPriority)
	SELECT TOP 1 'UCETNI_DATA', 1, Id, 1, 1, 10, 100
	  FROM ScheduledJob
	ORDER BY Id DESC;
END

SELECT * FROM ScheduledJob;
