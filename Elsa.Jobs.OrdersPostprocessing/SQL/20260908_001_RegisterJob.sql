IF NOT EXISTS(SELECT TOP 1 1 FROM ScheduledJob WHERE ModuleClass = 'Elsa.Jobs.OrdersPostprocessing.OrdersPostprocessingJob, Elsa.Jobs.OrdersPostprocessing')
BEGIN
	INSERT INTO ScheduledJob (ProjectId, Name, SecondsInterval, ModuleClass, CustomData, SequencePriority, ActiveFrom)
	VALUES (1, N'Orders postprocessing - tracking, upomínky', 1, 'Elsa.Jobs.OrdersPostprocessing.OrdersPostprocessingJob, Elsa.Jobs.OrdersPostprocessing', 'null', 1, GETDATE());

	INSERT INTO JobSchedule (ProjectId, ScheduledJobId, Active, CanBeStartedManually, Uid)
	SELECT TOP 1 ProjectId, Id, 1, 1, 'ORDERS_POSTPROCESSING'
	  FROM ScheduledJob
	 WHERE Id = SCOPE_IDENTITY();
END
