IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_BatchPriceComponent_BatchId')
	CREATE INDEX INX_BatchPriceComponent_BatchId	ON BatchPriceComponent	(BatchId);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_BatchPriceComponent_SourceBatchId')
	CREATE INDEX INX_BatchPriceComponent_SourceBatchId	ON BatchPriceComponent	(SourceBatchId);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_Customer_Email')
	CREATE INDEX INX_Customer_Email	ON Customer	(Email);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_ErpOrderStatusMapping_ErpStatusId')
	CREATE INDEX INX_ErpOrderStatusMapping_ErpStatusId	ON ErpOrderStatusMapping	(ErpStatusId);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_InvoiceForm_InvoiceFormCollectionId')
	CREATE INDEX INX_InvoiceForm_InvoiceFormCollectionId	ON InvoiceForm	(InvoiceFormCollectionId);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_InvoiceFormCollection_Month')
	CREATE INDEX INX_InvoiceFormCollection_Month	ON InvoiceFormCollection	(Month);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_InvoiceFormCollection_Year')
	CREATE INDEX INX_InvoiceFormCollection_Year	ON InvoiceFormCollection	(Year);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_InvoiceFormItem_InvoiceFormId')
	CREATE INDEX INX_InvoiceFormItem_InvoiceFormId	ON InvoiceFormItem	(InvoiceFormId);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_InvoiceFormItemMaterialBatch_InvoiceFormItemId')
	CREATE INDEX INX_InvoiceFormItemMaterialBatch_InvoiceFormItemId	ON InvoiceFormItemMaterialBatch	(InvoiceFormItemId);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_InvoiceFormItemMaterialBatch_MaterialBatchId')
	CREATE INDEX INX_InvoiceFormItemMaterialBatch_MaterialBatchId	ON InvoiceFormItemMaterialBatch	(MaterialBatchId);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_MaterialBatch_BatchNumber')
	CREATE INDEX INX_MaterialBatch_BatchNumber	ON MaterialBatch	(BatchNumber);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_MaterialBatch_CalculatedKey')
	CREATE INDEX INX_MaterialBatch_CalculatedKey	ON MaterialBatch	(CalculatedKey);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_MaterialBatch_InvoiceNr')
	CREATE INDEX INX_MaterialBatch_InvoiceNr	ON MaterialBatch	(InvoiceNr);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_MaterialBatch_InvoiceVarSymbol')
	CREATE INDEX INX_MaterialBatch_InvoiceVarSymbol	ON MaterialBatch	(InvoiceVarSymbol);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_MaterialBatch_IsAvailable')
	CREATE INDEX INX_MaterialBatch_IsAvailable	ON MaterialBatch	(IsAvailable);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_MaterialBatch_LockDt')
	CREATE INDEX INX_MaterialBatch_LockDt	ON MaterialBatch	(LockDt);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_MaterialBatch_MaterialId')
	CREATE INDEX INX_MaterialBatch_MaterialId	ON MaterialBatch	(MaterialId);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_MaterialBatchComposition_ComponentId')
	CREATE INDEX INX_MaterialBatchComposition_ComponentId	ON MaterialBatchComposition	(ComponentId);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_MaterialBatchComposition_CompositionId')
	CREATE INDEX INX_MaterialBatchComposition_CompositionId	ON MaterialBatchComposition	(CompositionId);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_MaterialComposition_ComponentId')
	CREATE INDEX INX_MaterialComposition_ComponentId	ON MaterialComposition	(ComponentId);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_MaterialComposition_CompositionId')
	CREATE INDEX INX_MaterialComposition_CompositionId	ON MaterialComposition	(CompositionId);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_OrderItem_KitParentId')
	CREATE INDEX INX_OrderItem_KitParentId	ON OrderItem	(KitParentId);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_OrderItem_PlacedName')
	CREATE INDEX INX_OrderItem_PlacedName	ON OrderItem	(PlacedName);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_OrderItem_PurchaseOrderId')
	CREATE INDEX INX_OrderItem_PurchaseOrderId	ON OrderItem	(PurchaseOrderId);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_OrderItemMaterialBatch_MaterialBatchId')
	CREATE INDEX INX_OrderItemMaterialBatch_MaterialBatchId	ON OrderItemMaterialBatch	(MaterialBatchId);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_OrderItemMaterialBatch_OrderItemId')
	CREATE INDEX INX_OrderItemMaterialBatch_OrderItemId	ON OrderItemMaterialBatch	(OrderItemId);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_Payment_VariableSymbol')
	CREATE INDEX INX_Payment_VariableSymbol	ON Payment	(VariableSymbol);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_PurchaseOrder_CustomerEmail')
	CREATE INDEX INX_PurchaseOrder_CustomerEmail	ON PurchaseOrder	(CustomerEmail);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_PurchaseOrder_ErpLastChange')
	CREATE INDEX INX_PurchaseOrder_ErpLastChange	ON PurchaseOrder	(ErpLastChange);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_PurchaseOrder_OrderNumber')
	CREATE INDEX INX_PurchaseOrder_OrderNumber	ON PurchaseOrder	(OrderNumber);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_PurchaseOrder_OrderStatusId')
	CREATE INDEX INX_PurchaseOrder_OrderStatusId	ON PurchaseOrder	(OrderStatusId);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_PurchaseOrder_VarSymbol')
	CREATE INDEX INX_PurchaseOrder_VarSymbol	ON PurchaseOrder	(VarSymbol);
GO
IF NOT EXISTS(SELECT TOP 1 1 FROM sys.indexes WHERE name='INX_UserSession_PublicId')
	CREATE INDEX INX_UserSession_PublicId	ON UserSession	(PublicId);
GO
