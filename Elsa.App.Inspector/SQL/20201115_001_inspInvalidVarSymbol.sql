IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'insp_invalidVarSymbol')
	DROP PROCEDURE insp_invalidVarSymbol;

GO

CREATE PROCEDURE inspDisabled_invalidVarSymbol (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN
			
    DECLARE @ordids TABLE (id INT, code NVARCHAR(200), text NVARCHAR(200));
	INSERT INTO @ordids
	SELECT po.Id, 'varsymmmatch_'+LTRIM(STR(po.Id)),
	  N'Objednávka č. ' + po.OrderNumber + N' VS: ' + po.VarSymbol + N' Č. Předf.:' + po.PreInvoiceId 
	  FROM PurchaseOrder po
	 WHERE po.ProjectId = @projectId
	   AND po.PurchaseDate > (GETDATE() - 31)
	   AND ((po.OrderNumber <> po.VarSymbol) OR (po.OrderNumber <> po.PreInvoiceId));
	
     WHILE(EXISTS(SELECT TOP 1 1 FROM @ordids))
	 BEGIN
		DECLARE @code NVARCHAR(200);
		DECLARE @message NVARCHAR(200);
		DECLARE @ordid INT;

		SELECT TOP 1 @ordid = id, @code = code, @message = text
		  FROM @ordids;	 	
		 	
		DECLARE @issueId INT;
		EXEC @issueId = inspfw_addIssue @sessionId, N'Nesouhlasící Číslo objednávky / VS / Č. předfaktury', @code, @message;
				
		EXEC inspfw_setIssueAction @issueId, '/UI/Inspector/ActionControls/PostponeOneDay.html', N'Odložit na zítra';
		EXEC inspfw_setIssueAction @issueId, '/UI/Inspector/ActionControls/Ignore.html', N'Ignorovat';
		
		DELETE FROM @ordids 
		 WHERE Id = @ordid;
	 END

END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'insp_varSymbolDuplicity')
	DROP PROCEDURE insp_varSymbolDuplicity;

GO

CREATE PROCEDURE insp_varSymbolDuplicity (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN

	DECLARE @vss TABLE (vs NVARCHAR(200), code NVARCHAR(200), text NVARCHAR(200));
	INSERT INTO @vss
    SELECT x.vs, 'duplvs_' + x.Vs, N'Více než jedna objednávka má variabilní symbol ' + x.vs 
	  FROM (
	SELECT po.VarSymbol vs, COUNT(DISTINCT po.OrderNumber) nu
	  FROM PurchaseOrder po
	 WHERE LEN(ISNULL(po.VarSymbol, '')) > 0 
	  AND po.ProjectId = @projectId
	  AND po.PurchaseDate > (GETDATE() - 31)
	GROUP BY po.VarSymbol	
	HAVING COUNT(po.OrderNumber) > 1) x;

     WHILE(EXISTS(SELECT TOP 1 1 FROM @vss))
	 BEGIN
		DECLARE @code NVARCHAR(200);
		DECLARE @message NVARCHAR(200);
		DECLARE @vs NVARCHAR(200);

		SELECT TOP 1 @vs = vs, @code = code, @message = text
		  FROM @vss;	 	
		 	
		DECLARE @issueId INT;
		EXEC @issueId = inspfw_addIssue @sessionId, N'Duplicitní variabilní symbol', @code, @message;
		
				
		DELETE FROM @vss 
		 WHERE vs = @vs;
	 END

END

