IF EXISTS(SELECT TOP 1 1 
			FROM sys.procedures
		   WHERE name = 'insp_erpCustomerGroupMappings')
	DROP PROCEDURE insp_erpCustomerGroupMappings;

GO

CREATE PROCEDURE insp_erpCustomerGroupMappings (@sessionId INT, @projectId INT, @retryIssueId INT = null)
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
    
	-- DECLARE @projectId INT = 1;

	with cte
	as
	(
		SELECT DISTINCT cg.ErpGroupName, 
			CASE WHEN cgt.Id IS NULL THEN 0 ELSE 1 END HasCustomerGroupType,
			CASE WHEN cgm.Id IS NULL THEN 0 ELSE 1 END HasCustomerGroupMapping
			from CustomerGroup cg
			join Customer c ON cg.CustomerId = c.Id
			left join CustomerGroupType cgt on cg.ErpGroupName = cgt.ErpGroupName
			left join CustomerGroupMapping cgm on cg.ErpGroupName = cgm.GroupErpName
		  WHERE c.ProjectId = @projectId
		    AND ISNULL(cgt.ProjectId, @projectId) = @projectId
			AND ISNULL(cgm.ProjectId, @projectId) = @projectId
   )
   SELECT N'Uživatelské skupiny bez nastavení v Else' "IssueType", 
         'noCGTp_' + c.ErpGroupName "IssueCode",
          N'Ve Floxu byla nalezena skupina "' + c.ErpGroupName + N'", která nemá nastavení v Else (CustomerGroupType)' "Message"  
     FROM cte c
	WHERE c.HasCustomerGroupType = 0
  /*UNION
    SELECT N'Uživatelské skupiny bez mapování pro CRM reporty v Else' "IssueType", 
         'noCGMap_' + c.ErpGroupName "IssueCode",
          N'Ve Floxu byla nalezena skupina "' + c.ErpGroupName + N'", která nemá mapping pro CRM reporty v Else (CustomerGroupMapping)' "Message"  
     FROM cte c
	WHERE c.HasCustomerGroupMapping = 0*/


END
GO


