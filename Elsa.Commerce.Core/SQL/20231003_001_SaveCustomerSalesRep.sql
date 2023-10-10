IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'SaveCustomerSalesRep')
	DROP PROCEDURE SaveCustomerSalesRep;

GO

CREATE PROCEDURE [dbo].[SaveCustomerSalesRep](@projectId INT, @userId INT, @customerId INT, @salesRepEmail nvarchar(100))
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

	-- End validity of a record for the same customer but another SR
	UPDATE SalesRepCustomer
	   SET ValidTo = GETDATE()
	 WHERE ValidTo IS NULL
	   AND ValidFrom < GETDATE()
	   AND SalesRepId <> ISNULL(@srepId, -1)
	   AND CustomerId = @customerId;

     IF (@salesRepEmail IS NOT NULL)
	 BEGIN
		INSERT INTO SalesRepCustomer (SalesRepId, CustomerId, ValidFrom, InsertUserId)
		SELECT @srepId, @customerId, GETDATE(), @userId
		 WHERE NOT EXISTS(SELECT TOP 1 1 
		                    FROM SalesRepCustomer src
						   WHERE src.CustomerId = @customerId
						     AND src.SalesRepId = @srepId
							 AND src.ValidFrom <= GETDATE()
							 AND src.ValidTo IS NULL);
	END
END