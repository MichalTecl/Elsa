IF EXISTS(SELECT TOP 1 1 
            FROM sys.procedures
		   WHERE name = 'insp_abandonedBatchRemains')
BEGIN
	DROP PROCEDURE insp_abandonedBatchRemains;
END

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

	with cte as
	(
		SELECT mi.Name InventoryName,
			   m.Id MaterialId,
			   m.Name MaterialName,
			   bam.BatchNumber, 
			   CAST(ROUND(bam.Available / bam.BatchTotal * 100, 0) AS INT) RemainingPercent,
			   batchEvent.LastBatchEvent,
			   bam.Available,
			   bam.UnitId			   
		  FROM vwBatchKeyAvailableAmount bam
		  JOIN Material m ON (bam.MaterialId = m.Id)
		  JOIN MaterialInventory mi ON (m.InventoryId = mi.Id)
		  JOIN (
			SELECT eba.BatchNumber, 
				   eba.MaterialId, 
				   MAX(be.EventDt) LastBatchEvent
			  FROM vwBatchEvent be
			  JOIN MaterialBatch eba ON (be.BatchId = eba.Id)			 
			 GROUP BY eba.BatchNumber, eba.MaterialId
		  ) batchEvent ON (batchEvent.MaterialId = m.Id AND batchEvent.BatchNumber = bam.BatchNumber)
		 WHERE m.AutomaticBatches = 0
		   AND m.ProjectId = @projectId
		   AND bam.Available > 0
		   AND ((@retryBatchNr IS NULL) OR (bam.BatchNumber = @retryBatchNr))
		   AND ((@retryMaterialId IS NULL) OR (bam.MaterialId = @retryMaterialId))
	)

	/*
 DECLARE @isu TABLE (isnpType NVARCHAR(200), 
                    code NVARCHAR(200), 
					message NVARCHAR(1000),
					materialId INT,
					amount DECIMAL,
					amountUnitId INT);

					select * from vwBatchEvent
 
 */

	INSERT INTO @isu
	SELECT DISTINCT 
	       N'Opuštěné ' + c.InventoryName,  
		   'abandBatch_' + c.BatchNumber + '_' + LTRIM(STR(c.MaterialId)),
		   N'Šarže ' + c.BatchNumber + '  - "' + c.MaterialName 
		 + N'" se zdá být již nepoužívána - zbývá méně než ' + LTRIM(STR(c.RemainingPercent)) + N'% celkového množství a již byla používána jiná šarže stejného materiálu',
		 c.MaterialId,
		 c.Available,
		 c.UnitId,
		 c.BatchNumber,
		 mu.Symbol
	  FROM cte c
	  JOIN (SELECT beb.MaterialId, beb.BatchNumber, MAX(be.EventDt) LastEvent
			  FROM vwBatchEvent be
			  JOIN MaterialBatch beb ON (be.BatchId = beb.Id)
			 WHERE be.EventName IN ( 'USED_AS_COMPONENT', 'ESHOP_SALE', 'DIRECT_SALE_ALLOCATION')
			GROUP BY  beb.MaterialId, beb.BatchNumber
			  ) materialLastEvent ON (c.MaterialId = materialLastEvent.MaterialId)  
	  JOIN MaterialUnit mu ON (c.UnitId = mu.Id)
	WHERE materialLastEvent.LastEvent > c.LastBatchEvent
	  AND materialLastEvent.BatchNumber <> c.BatchNumber
	  AND c.RemainingPercent < 10;

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

