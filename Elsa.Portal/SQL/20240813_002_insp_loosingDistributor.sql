IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'insp_leavingValuableDistributor')
	DROP PROCEDURE insp_leavingValuableDistributor;

GO

declare @isu table (id int, typeId int);
insert into @isu
select isu.id, isu.InspectionTypeId 
  from inspectionissue isu
 where isu.IssueCode like N'leavingValDist%';

 delete from InspectionIssueData where IssueId in (select id from @isu);
 delete from InspectionIssueActionsHistory where InspectionIssueId in (select id from @isu);
 delete from InspectionIssueActionMenu where IssueId in (select id from @isu);
 delete from InspectionIssue where id in (select id from @isu);
 delete from InspectionType where id in (select id from @isu);

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'insp_loosingDistributor')
	DROP PROCEDURE insp_loosingDistributor;

GO

CREATE PROCEDURE [insp_loosingDistributor] (@sessionId INT, @projectId INT, @retryIssueId INT = null)
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
    
	-- DECLARE @projectId INT = 1

	SELECT N'VO na odchodu' IssueType,
	       'loosingdistributor_' + LTRIM(STR(x.id)) IssueCode,
		    x.Name + N' - více než ' + LTRIM(STR(x.lastOrderMonths)) + N' měsíců od poslední objednávky' [Message],
			'/UI/Inspector/ActionControls/PostponeOneMonth.html' "ActionControlUrl_Postpone1M",
			N'Připomenout za měsíc' "ActionName_Postpone1M",
			'/UI/Inspector/ActionControls/CRM/SnoozeDistributor.html' "ActionControlUrl_Snooze",
			N'Ignorovat do další objednávky' "ActionName_Snooze",
			x.Id "data:CustomerId"
		  FROM
			(SELECT
	          c.Id, c.Name, DATEDIFF(month, lor.LatestSuccessOrderDt, GETDATE()) lastOrderMonths
			FROM Customer c	  
			JOIN vwCustomerLatestOrder lor ON (lor.CustomerId = c.Id)
			WHERE c.ProjectId = @projectId
			AND c.IsDistributor = 1
			AND c.IsCompany = 1
			AND c.Id NOT IN (SELECT CustomerId FROM vwSnoozedDistributors)) x
		WHERE x.lastOrderMonths BETWEEN 4 AND 6

END
GO
