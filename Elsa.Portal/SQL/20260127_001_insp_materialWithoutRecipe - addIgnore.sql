CREATE OR ALTER PROCEDURE insp_materialWithoutRecipe (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN
		
	/*
	const string issueTypeColumn = "IssueType";
	const string issueCodeColumn = "IssueCode";
	const string messageColumn = "Message";
	const string issueDataPrefix = "data:";
	const string actionControlPrefix = "ActionControlUrl";
	const string actionNamePrefix = "ActionName";
	*/

	SELECT N'Materiál bez receptury' IssueType,
	       'materialWoRecipe' + LTRIM(STR(m.Id)) IssueCode,
		   N'Materiál "' + m.Name + N'" není použit v žádné receptuře (a stav skladu není 0)' [Message], 
		   '/UI/Inspector/ActionControls/PostponeOneMonth.html' "ActionControlUrl_Postpone1M",
			N'Odložit o měsíc' "ActionName_Postpone1M",
            '/UI/Inspector/ActionControls/Ignore.html' "ActionControlUrl_Ignore",
			N'Vyřešeno - ignorovat' "ActionName_Ignore"
	  FROM Material m
	  JOIN MaterialInventory mi ON (m.InventoryId = mi.Id)
	WHERE mi.CanBeConnectedToTag = 0
	  AND EXISTS(SELECT TOP 1 1
	              FROM vwBatchAvailableAmount bam
				 WHERE bam.MaterialId = m.Id
				   AND bam.Available > 0)
	  AND NOT EXISTS(SELECT TOP 1 1
	                   FROM RecipeComponent rc
					  WHERE rc.MaterialId = m.Id)
				 
	

END
GO


