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

CREATE PROCEDURE [dbo].[insp_loosingDistributor] (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN
					
    DECLARE @dists TABLE (id INT, name NVARCHAR(300), monthsAgo int);

	INSERT INTO @dists
	SELECT *
	  FROM (
		SELECT c.Id, c.Name, DATEDIFF(month, lor.LatestSuccessOrderDt, GETDATE()) lastOrderMonths
			  FROM Customer c	  
			  JOIN vwCustomerLatestOrder lor ON (lor.CustomerId = c.Id)
			 WHERE c.ProjectId = @projectId
			   AND c.IsDistributor = 1
			   AND c.IsCompany = 1) x
			WHERE x.lastOrderMonths BETWEEN 4 AND 6;
		 	 		
     WHILE(EXISTS(SELECT TOP 1 1 FROM @dists))
	 BEGIN
		DECLARE @code NVARCHAR(100);
		DECLARE @message NVARCHAR(2000);
		DECLARE @custid INT;
		DECLARE @name NVARCHAR(200);
		DECLARE @months INT;

		SELECT TOP 1 @custId = Id, @name = name, @months = monthsAgo FROM @dists;
		DELETE FROM @dists WHERE Id = @custid;
									
		SET @code = 'loosingdistributor_' + LTRIM(STR(@custid));
		SET @message = N'VO ' + @name + N' už ' + @months + N' měsíců nic neobjednal.';

		DECLARE @issueId INT;
		EXEC @issueId = inspfw_addIssue @sessionId, N'VO na odchodu', @code, @message;
				
		EXEC inspfw_setIssueAction @issueId, '/UI/Inspector/ActionControls/PostponeOneMonth.html', N'Připomenout za měsíc';
			
	 END

END
GO


