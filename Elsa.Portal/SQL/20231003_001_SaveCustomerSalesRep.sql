IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'SaveCustomerSalesRep')
	DROP PROCEDURE SaveCustomerSalesRep;

GO

CREATE PROCEDURE SaveCustomerSalesRep(@projectId INT, @userId INT, @customerId INT, @salesRepEmail nvarchar(100))
AS
BEGIN
	
	DECLARE @srepId INT;

	SET @srepId = (SELECT TOP 1 Id FROM SalesRepresentative sr WHERE sr.ProjectId = @projectId AND sr.NameInErp = @salesRepEmail);
	IF (@srepId IS NULL AND @salesRepemail IS NOT NULL)
	BEGIN
		INSERT INTO SalesRepresentative (NameInErp, PublicName, ProjectId)
		VALUES (@salesRepEmail, @salesRepEmail, @projectId);

		SET @srepId = SCOPE_IDENTITY();
	END

	UPDATE SalesRepCustomer
	   SET ValidTo = GETDATE()
	 WHERE ValidTo IS NULL
	   AND ValidFrom < GETDATE()
	   AND SalesRepId = @srepId
	   AND CustomerId = @customerId;

     IF (@salesRepEmail IS NOT NULL)
	 BEGIN
		INSERT INTO SalesRepCustomer (SalesRepId, CustomerId, ValidFrom, InsertUserId)
		VALUES (@srepId, @customerId, GETDATE(), @userId);
	END
END