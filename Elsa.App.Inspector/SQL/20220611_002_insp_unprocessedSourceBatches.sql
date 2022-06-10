IF EXISTS(SELECT TOP 1 1 
            FROM sys.procedures
		   WHERE name = 'insp_unprocessedSourceBatches')
BEGIN
	DROP PROCEDURE insp_unprocessedSourceBatches;
END

GO

CREATE PROCEDURE insp_unprocessedSourceBatches (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN
	
	DECLARE @retryBatchId INT = (SELECT TOP 1 ida.IntValue
	                               FROM InspectionIssueData ida
								  WHERE ida.IssueId = @retryIssueId
								    AND ida.PropertyName = 'BatchId');

    DECLARE @isu TABLE 
	  (BatchId INT,	   
	   Code NVARCHAR(200),
	   Msg NVARCHAR(1000));
    INSERT INTO @isu (BatchId, Code, Msg)
	SELECT mb.Id, 
	       'unprcBatch_' + LTRIM(STR(mb.Id)),
	      N'Šarže ' + mb.BatchNumber + ' "' + m.Name + N'" zbývá ' + LTRIM(STR(dbo.ToInt(bamp.AvailablePercent))) + N'% v nezpracovaném stavu'
	  FROM Material m
	  JOIN vwBatchAvailableAmountWithPercentage bamp ON (bamp.MaterialId = m.Id)
	  JOIN MaterialBatch mb ON (bamp.BatchId = mb.Id)	  
	 WHERE m.ProjectId = @projectId
	   AND m.DaysBeforeWarnForUnused IS NOT NULL
	   AND bamp.AvailablePercent > 33
	   AND (DATEDIFF(DAY, mb.Created, GETDATE())) > m.DaysBeforeWarnForUnused
	   AND bamp.ProjectId = @projectId
	   AND m.Id IN (SELECT c.MaterialId FROM RecipeComponent c)
	   AND ((@retryBatchId IS NULL) OR (mb.Id = @retryBatchId));

     
	 DECLARE @BatchId INT, @Code NVARCHAR(200), @Msg NVARCHAR(1000);
     
	 WHILE(EXISTS(SELECT TOP 1 1 FROM @isu))
	 BEGIN
		SELECT TOP 1 @BatchId = i.BatchId,
		             @Code = i.Code,
					 @Msg = i.Msg
   		 FROM @isu i;
		 DELETE FROM @isu WHERE BatchId = @BatchId;

		 DECLARE @issueType NVARCHAR(200) = N'Nezpracované ' + (SELECT TOP 1 ISNULL(m.UnusedWarnMaterialType, mi.Name) 
		                                      FROM MaterialBatch mb
											  JOIN Material m ON (mb.MaterialId = m.Id)
											  JOIN MaterialInventory mi ON (m.InventoryId = mi.Id)
											 WHERE mb.Id = @BatchId);
         
		 DECLARE @issueId INT;
		 EXEC @issueId = inspfw_addIssue @sessionId, @issueType, @code, @msg;

		 EXEC inspfw_setIssueDataInt @issueId, 'BatchId', @batchId;
		 EXEC inspfw_setIssueAction @issueId, '/UI/Inspector/ActionControls/PostponeOneDay.html', N'Odložit na zítra';
		 EXEC inspfw_setIssueAction @issueId, '/UI/Inspector/ActionControls/PostponeOneWeek.html', N'Odložit o týden';
	 END

END
   

