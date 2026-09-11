DECLARE @GroupName NVARCHAR(256) = N'Selhani importu objednavek';
DECLARE @Addresses NVARCHAR(MAX) = N'mtecl.prg@gmail.com';

INSERT INTO EmailRecipientList (ProjectId, GroupName, Addresses)
SELECT p.Id, @GroupName, @Addresses
  FROM Project p
 WHERE NOT EXISTS (
           SELECT TOP 1 1
             FROM EmailRecipientList erl
            WHERE erl.ProjectId = p.Id
              AND erl.GroupName = @GroupName
       );

