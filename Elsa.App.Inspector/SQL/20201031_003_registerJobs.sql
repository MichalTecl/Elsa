IF NOT EXISTS(SELECT TOP 1 1 FROM ScheduledJob WHERE Name = 'Inspektor')
BEGIN
	INSERT INTO ScheduledJob (ProjectId, Name, SecondsInterval, ModuleClass, SequencePriority, ActiveFrom, CustomData)
	VALUES (1, 'Inspektor', 10, 'Elsa.App.Inspector.Jobs.InspectorJob, Elsa.App.Inspector', 1, GETDATE(), 'null');

	DECLARE @schid INT = SCOPE_IDENTITY();

	INSERT INTO JobSchedule (ProjectId, ScheduledjobId, Active, CanBeStartedManually, Uid)
	VALUES (1, @schid, 1, 1, 'INSPEKTOR');
END