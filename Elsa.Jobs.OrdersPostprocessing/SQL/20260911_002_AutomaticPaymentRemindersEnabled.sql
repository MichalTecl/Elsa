INSERT INTO SysConfig (ProjectId, [Key], ValueJson, ValidFrom, InsertUserId)
SELECT p.Id, 'OrdersPostprocessing.AutomaticPaymentRemindersEnabled', 'false', GETDATE(), 2
  FROM Project p
 WHERE NOT EXISTS(SELECT TOP 1 1
                    FROM SysConfig sc
                   WHERE sc.ProjectId = p.Id
                     AND sc.[Key] = 'OrdersPostprocessing.AutomaticPaymentRemindersEnabled');
