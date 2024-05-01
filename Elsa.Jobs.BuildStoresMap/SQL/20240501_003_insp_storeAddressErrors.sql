IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'insp_storeAddressError')
	DROP PROCEDURE insp_storeAddressError;

GO

CREATE PROCEDURE [dbo].[insp_storeAddressError] (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN
	
	DECLARE @isu TABLE (customerId INT, customerName NVARCHAR(255), issue NVARCHAR(255));
		WITH cte
		AS
		(
			SELECT cs.CustomerId, 'Adresa' err FROM CustomerStore cs WHERE LEN(TRIM(ISNULL(cs.Address, ''))) < 3 
			UNION
			SELECT cs.CustomerId, N'Město' err FROM CustomerStore cs WHERE LEN(TRIM(ISNULL(cs.City, ''))) < 3
			UNION
			SELECT cs.CustomerId, N'WWW' err FROM CustomerStore cs WHERE LEN(TRIM(ISNULL(cs.Www, ''))) < 3
			UNION
			SELECT cs.CustomerId, N'GPS' err FROM CustomerStore cs WHERE LEN(TRIM(ISNULL(cs.Lat, ''))) < 3 OR LEN(TRIM(ISNULL(cs.Lon, ''))) < 3
		)

			INSERT INTO @isu
		SELECT c.Id, c.Name, errs.err
		  FROM Customer c
		  JOIN (SELECT cte.CustomerId, STRING_AGG(err, ', ') err
				  FROM cte  
				 GROUP BY cte.CustomerId) errs ON (c.Id = errs.CustomerId)
		 WHERE c.ProjectId = @projectId
		   AND c.IsValuableDistributor = 1;
		   		   	   		    	
     WHILE(EXISTS(SELECT TOP 1 1 FROM @isu))
	 BEGIN
		DECLARE @customerId INT,
				@customerName NVARCHAR(255),
				@issues NVARCHAR(255);

		SELECT TOP 1
			@customerId = customerId,
			@customerName = customerName,
			@issues = issue
		FROM
			@isu;

		DECLARE @code NVARCHAR(200);
		DECLARE @message NVARCHAR(2000);
			
		SET @code = 'storeAddressError_' + LTRIM(STR(@customerId));
		SET @message = N'K prodejně zákazníka "' + ISNULL(@customerName, '?') + N'" chybí tato povinná pole: ' + @issues;
		 	
		DECLARE @issueId INT;
		EXEC @issueId = inspfw_addIssue @sessionId, N'Nekompletní adresa prodejny', @code, @message;

		DELETE FROM @isu WHERE customerId = @customerId;
	 END
END
GO


