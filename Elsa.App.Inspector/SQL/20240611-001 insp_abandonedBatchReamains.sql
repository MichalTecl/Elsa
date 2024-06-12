IF EXISTS(SELECT TOP 1 1 
            FROM sys.procedures
		   WHERE name = 'insp_abandonedBatchRemains')
BEGIN
	DROP PROCEDURE insp_abandonedBatchRemains;
END

GO


CREATE PROCEDURE [dbo].[insp_abandonedBatchRemains] (@sessionId INT, @projectId INT, @retryIssueId INT = null)
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

	--DECLARE @projectId INT = 1;

	 
	DECLARE @stockEventTypeId INT;
	DECLARE @stockEventTypeName NVARCHAR(200);
	SELECT TOP 1 @stockEventTypeId = Id, @stockEventTypeName = Name FROM StockEventType WHERE RequiresNote = 1 AND ProjectId = @projectId;
	
	/*
	const string issueTypeColumn = "IssueType";
	const string issueCodeColumn = "IssueCode";
	const string messageColumn = "Message";
	const string issueDataPrefix = "data:";
	const string actionControlPrefix = "ActionControlUrl";
	const string actionNamePrefix = "ActionName";
	*/

	SELECT DISTINCT 
	       N'Opuštěné ' + m.UnusedWarnMaterialType [IssueType],
		   'abandonedBatch2_' + c.BatchNumber + '_' + LTRIM(STR(c.MaterialId)) [IssueCode],
		   N'Šarže ' + c.BatchNumber + '  - "' + c.MaterialName + N'" nebyla použita ' + LTRIM(STR(c.DaysFromEvent)) + N' dnů' + 
		   CASE WHEN c.NotAbandonedUntilNewerBatchUsed = 1 THEN ' a již byla použita novější šarže "' + mostRecB.BatchNumber + '"' ELSE '.' END [Message],
		   c.MaterialId  "data:MaterialId",
		   bam.Available "data:Amount",
		   bam.UnitId    "data:AmountUnitId",
		   u.Symbol      "data:AmtUnitSymbol",
		   c.BatchNumber "data:BatchNumber",
		   N'Odepsat ' + LTRIM(STR(bam.Available)) + ' ' + u.Symbol + N' šarže ' + c.BatchNumber + ' jako ' + @stockEventTypeName "data:StockEventText",
		   @stockEventTypeId "data:PrefStEventTypeId",
		   @stockEventTypeName "data:PrefStEventTypeName",
		   
		   '/UI/Inspector/ActionControls/PostponeOneWeek.html' [ActionControlUrl1],
		   N'Odložit o týden' [ActionName1],

		   '/UI/Inspector/ActionControls/PostponeOneMonth.html' [ActionControlUrl2],
		   N'Odložit o měsíc' [ActionName2],

		   '/UI/Controls/Inventory/WarehouseControls/WhActions/StockEventThrashActionButton.html' [ActionControlUrl3],
		   @stockEventTypeName [ActionName3]

	
	    FROM vwAbandonedBatches c
	    JOIN Material m ON (c.MaterialId = m.Id)
		JOIN vwBatchAvailableAmount bam ON (c.BatchId = bam.BatchId)
		JOIN MaterialUnit u ON (bam.UnitId = u.Id)
		LEFT JOIN MaterialBatch mostRecB ON (c.MostRecentBatch = mostRecB.Id)
	  WHERE c.ProjectId = @projectId
	    AND m.UnusedWarnMaterialType IS NOT NULL
		AND c.IsAbandoned = 1
		AND ((@retryBatchNr IS NULL) OR (c.BatchNumber = @retryBatchNr))
		AND ((@retryMaterialId IS NULL) OR (c.MaterialId = @retryMaterialId));

END
GO


