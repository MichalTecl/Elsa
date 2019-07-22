DECLARE @projectId INT = 1;


block:

	DECLARE @eventId INT;
	DECLARE @counterPrefix VARCHAR(3);
	DECLARE @counterName VARCHAR(100);

	SET @eventId = NULL;

	SELECT TOP 1 @eventId = et.Id, @counterPrefix = 'V' + UPPER(SUBSTRING(et.Name, 1, 3)), @counterName = 'StockEvent_' + et.Name
	  FROM StockEventType et
	 WHERE et.ProjectId = @projectId
	   AND et.InvoiceFormNumberCounterId IS NULL

	IF (@eventId IS NULL)
	BEGIN
	    PRINT 'hotovo';
		GOTO done;
	END

	PRINT @counterName;

	DECLARE @newCounterId INT;

	SET @newCounterId = (SELECT TOP 1 Id FROM SystemCounter WHERE Name = @counterName);

	PRINT @counterPrefix;

	IF (@newCounterId IS NULL)
	BEGIN
		INSERT INTO SystemCounter (Name, StaticPrefix, DtFormat, CounterValue, CounterMinValue, CounterPadding, ProjectId)
		VALUES (@counterName, @counterPrefix, 'yyyy', 0, 1, 5, @projectId);	

		SET @newCounterId = SCOPE_IDENTITY();
	END

	UPDATE StockEventType
	   SET InvoiceFormNumberCounterId = @newCounterId
	 WHERE Id = @eventId;

GOTO block;
done:


