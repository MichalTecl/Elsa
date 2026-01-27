CREATE OR ALTER PROCEDURE insp_customerTagTimeouted (@sessionId INT, @projectId INT, @retryIssueId INT = null)
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


	SELECT N'Štítek "' + ctt.Name + N'" po deadline' IssueType, 
		   'crmtagtout' + LTRIM(STR(ctt.Id)) + '.' + LTRIM(STR(c.Id)) + '.' + LTRIM(STR(ctt.DaysToWarning)) IssueCode,
		   N'Štítek "' 
			+ ctt.Name 
			+ N'" přiřazen k "' 
			+ c.Name 
			+ N'" déle než povolených ' 
			+ LTRIM(STR(ctt.DaysToWarning)) 
			+ N' dnů' Message,
		  c.Id "data:CustomerId",
		  N'/UI/Inspector/ActionControls/CRM/OpenCustomerCard.html' "ActionControlUrl_Open",
		  N'Otevřít kartu VO' "ActionName_Open"
	  FROM CustomerTagAssignment cta
	  JOIN CustomerTagType ctt ON (cta.TagTypeId = ctt.Id)
	  JOIN Customer c ON (cta.CustomerId = c.Id)
	WHERE ctt.DaysToWarning IS NOT NULL
	  AND DATEADD(day, ctt.DaysToWarning, cta.AssignDt) < GETDATE();

END