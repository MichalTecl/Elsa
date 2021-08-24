IF NOT EXISTS(SELECT TOP 1 1 FROM SysConfig WHERE [Key] = 'OrderProcessing.PaymentMethodsToSetPaidAuto')
BEGIN
    INSERT INTO SysConfig (ProjectId, [Key], ValueJson, ValidFrom, InsertUserId)
     VALUES (1, 'OrderProcessing.PaymentMethodsToSetPaidAuto', N'["Faktura - 14denní splatnost"]', GETDATE(), 2);
END