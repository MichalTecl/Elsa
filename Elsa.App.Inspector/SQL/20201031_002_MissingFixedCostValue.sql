IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'insp_missingFixedCosts')
	DROP PROCEDURE insp_missingFixedCosts;

GO

CREATE PROCEDURE insp_missingFixedCosts (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN
	
	IF ((@retryIssueId IS NULL) AND (DAY(GETDATE()) < 15))
	BEGIN
		RETURN;
	END

	DECLARE @yr INT = YEAR(GETDATE());
	DECLARE @mo INT = MONTH(GETDATE());
	
	DECLARE @isu TABLE (code NVARCHAR(300), message NVARCHAR(300), tid INT)

	INSERT INTO @isu		
    SELECT 'missingFixCost_' + LTRIM(STR(@yr)) + '/' + LTRIM(STR(@mo)) + '_' + fct.Name,
	       N'Není zadána částka "' + fct.Name + '" pro ' + LTRIM(STR(@mo)) + '/' + LTRIM(STR(@yr)),
		   fct.Id
	  FROM FixedCostType fct
	 WHERE fct.ProjectId = @projectId
	   AND NOT EXISTS(SELECT TOP 1 1 
	                    FROM FixedCostValue fcv
					   WHERE fcv.FixedCostTypeId = fct.Id
					     AND fcv.Year = @yr
						 AND fcv.Month = @mo); 
					 
			
     WHILE(EXISTS(SELECT TOP 1 1 FROM @isu))
	 BEGIN
		DECLARE @code NVARCHAR(100);
		DECLARE @message NVARCHAR(2000);
		DECLARE @tid INT;

		SELECT TOP 1 @tid = tid, @code = code, @message = message
		  FROM @isu;
		 	
		DECLARE @issueId INT;
		EXEC @issueId = inspfw_addIssue @sessionId, N'Nepřímé náklady', @code, @message;

		EXEC inspfw_setIssueDataInt @issueId, 'FixedcostTypeId', @tid;
		EXEC inspfw_setIssueDataInt @issueId, 'Month', @mo;
		EXEC inspfw_setIssueDataInt @issueId, 'Year', @yr;

		EXEC inspfw_setIssueAction @issueId, '/UI/Inspector/ActionControls/PostponeOneWeek.html', N'Odložit o týden';
		EXEC inspfw_setIssueAction @issueId, '/UI/Controls/Accounting/FixedCostIssueActionButton.html', N'Zadat';
		

		DELETE FROM @isu 
		 WHERE tid = @tid;
	 END

END