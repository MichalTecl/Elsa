IF NOT EXISTS(SELECT TOP 1 1 FROM SysConfig WHERE [Key] = 'OrderProcessing.OrderWeightAddition')
BEGIN
    INSERT INTO SysConfig (ProjectId, [Key], ValueJson, ValidFrom, InsertUserId)
     VALUES (1, 'OrderProcessing.OrderWeightAddition', '0.7', GETDATE(), 2);
END