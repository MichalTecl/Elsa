IF EXISTS(SELECT TOP 1 1 
            FROM sys.procedures
		   WHERE name = 'insp_abandonedBatchRemains')
BEGIN
	DROP PROCEDURE insp_abandonedBatchRemains;
END

GO

UPDATE InspectionIssue 
   SET ResolveDt = GETDATE()
 WHERE IssueCode LIKE 'abandBatch%'   
   ANd ResolveDt IS NULL;

GO

CREATE PROCEDURE insp_abandonedBatchRemains (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN
	
	DECLARE @retryBatchNr NVARCHAR(200) = (SELECT TOP 1 ida.StrValue
	                               FROM InspectionIssueData ida 
								  WHERE ida.IssueId = @retryIssueId
								    AND ida.PropertyName = 'BatchNumber');
	DECLARE @retryMaterialId INT = (SELECT TOP 1 ida.IntValue
	                                  FROM InspectionIssueData ida
									 WHERE ida.IssueId = @retryIssueId
									   AND ida.PropertyName = 'MaterialId');  

	 
	DECLARE @isu TABLE (inspType NVARCHAR(200), 
                code NVARCHAR(200), 
				message NVARCHAR(1000),
				materialId INT,
				amount DECIMAL,
				amountUnitId INT,
				batchNr NVARCHAR(200),
				amtUnitSymbol NVARCHAR(20));
	

	INSERT INTO @isu
	SELECT DISTINCT 
	       N'Opuštěné ' + m.UnusedWarnMaterialType,  --inspType
		   'abandonedBatch2_' + c.BatchNumber + '_' + LTRIM(STR(c.MaterialId)), --code
		   N'Šarže ' + c.BatchNumber + '  - "' + c.MaterialName + N'" nebyla použita ' + LTRIM(STR(c.DaysFromEvent)) + N' dnů', --message
		 c.MaterialId, -- materialId
		 bam.Available, -- amount
		 bam.UnitId, -- amountUnitId
		 c.BatchNumber, -- batchNr
		 u.Symbol -- amtUnitSymbol
	    FROM vwAbandonedBatches c
	    JOIN Material m ON (c.MaterialId = m.Id)
		JOIN vwBatchAvailableAmount bam ON (c.BatchId = bam.BatchId)
		JOIN MaterialUnit u ON (bam.UnitId = u.Id)
	  WHERE c.ProjectId = @projectId
	    AND m.UnusedWarnMaterialType IS NOT NULL;

	 DECLARE @stockEventTypeId INT;
	 DECLARE @stockEventTypeName NVARCHAR(200);
	 SELECT TOP 1 @stockEventTypeId = Id, @stockEventTypeName = Name FROM StockEventType WHERE RequiresNote = 1 ANd ProjectId = @projectId;
	 		 
     WHILE(EXISTS(SELECT TOP 1 1 FROM @isu))
	 BEGIN
	    DECLARE @inspType NVARCHAR(200);                    
		DECLARE @materialId INT;
		DECLARE	@amount DECIMAL;
		DECLARE @amountUnitId INT;
		DECLARE @code NVARCHAR(100);
		DECLARE @message NVARCHAR(2000);
		DECLARE @batchNr NVARCHAR(200);
		DECLARE @unitSymbol NVARCHAR(20);

		SELECT TOP 1 @code = code,
					 @inspType = inspType,
					 @materialId = materialId,
					 @amount = amount,
					 @amountUnitId = amountUnitId,
					 @message = message,
					 @batchNr = batchNr,
					 @unitSymbol = amtUnitSymbol  
		  FROM @isu;
  		 	
		DECLARE @issueId INT;
		EXEC @issueId = inspfw_addIssue @sessionId, @inspType, @code, @message;

		EXEC inspfw_setIssueDataString @issueId, 'BatchNumber', @batchNr;
		EXEC inspfw_setIssueDataString @issueId, 'Amount', @amount;
		EXEC inspfw_setIssueDataInt @issueId, 'AmountUnitId', @amountUnitId;
		EXEC inspfw_setIssueDataInt @issueId, 'MaterialId', @materialId;
		EXEC inspfw_setIssueDataString @issueId, 'AmtUnitSymbol', @unitSymbol;
		
		EXEC inspfw_setIssueAction @issueId, '/UI/Inspector/ActionControls/PostponeOneWeek.html', N'Odložit o týden';

		IF (@stockEventTypeId IS NOT NULL)
		BEGIN
			DECLARE @stevtText NVARCHAR(1000) = N'Odepsat ' + LTRIM(STR(@amount)) + ' ' + @unitSymbol + N' šarže ' + @batchNr + ' jako ' + @stockEventTypeName;

			EXEC inspfw_setIssueDataString @issueId, 'StockEventText', @stevtText;
			EXEC inspfw_setIssueDataInt @issueId, 'PrefStEventTypeId', @stockEventTypeId;
			EXEC inspfw_setIssueDataString @issueId, 'PrefStEventTypeName', @stockEventTypeName;
			EXEC inspfw_setIssueAction @issueId, '/UI/Controls/Inventory/WarehouseControls/WhActions/StockEventThrashActionButton.html', @stockEventTypeName;
		END

		DELETE FROM @isu WHERE Code = @code;
	 END
END
GO

