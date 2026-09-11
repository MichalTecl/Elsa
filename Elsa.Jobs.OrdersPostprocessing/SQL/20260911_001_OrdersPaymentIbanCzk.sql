INSERT INTO SysConfig (ProjectId, [Key], ValueJson, ValidFrom, InsertUserId)
SELECT p.Id, 'OrdersPostprocessing.PaymentIbanCzk', N'"CZ5220100000002301656475"', GETDATE(), 2
  FROM Project p
 WHERE NOT EXISTS(SELECT TOP 1 1
                    FROM SysConfig sc
                   WHERE sc.ProjectId = p.Id
                     AND sc.[Key] = 'OrdersPostprocessing.PaymentIbanCzk');
